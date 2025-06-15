using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Runtime.CompilerServices;
using AGUIDotnet.Agent;
using AGUIDotnet.Events;
using AGUIDotnet.Types;
using Microsoft.Extensions.AI;
using Xunit;

namespace AGUIDotnet.Tests;

public class IntegrationTests
{
    private static RunAgentInput CreateRunAgentInput(List<BaseMessage> messages)
    {
        return new RunAgentInput
        {
            ThreadId = "thread_123",
            RunId = "run_123",
            State = JsonSerializer.SerializeToElement(new { count = 0 }),
            Messages = messages.ToImmutableList(),
            Tools = new List<Tool>
            {
                new Tool
                {
                    Name = "get_weather",
                    Description = "Get the weather for a location",
                    Parameters = JsonSerializer.SerializeToElement(new
                    {
                        type = "object",
                        properties = new
                        {
                            location = new
                            {
                                type = "string",
                                description = "The location to get weather for"
                            }
                        },
                        required = new[] { "location" }
                    })
                }
            }.ToImmutableList(),
            Context = ImmutableList<Context>.Empty,
            ForwardedProps = JsonSerializer.SerializeToElement(new { })
        };
    }

    [Fact]
    public async Task TestEchoAgent()
    {
        // Arrange
        var agent = new EchoAgent();
        var messages = new List<BaseMessage>
        {
            new SystemMessage
            {
                Id = "sys_1",
                Content = "You are a helpful assistant."
            },
            new UserMessage
            {
                Id = "user_1",
                Content = "Hello!"
            }
        };

        var events = new List<BaseEvent>();
        var channel = Channel.CreateUnbounded<BaseEvent>();
        var writer = channel.Writer;
        var reader = channel.Reader;

        // Act
        var runTask = agent.RunAsync(CreateRunAgentInput(messages), writer);
        var completedTask = await Task.WhenAny(runTask, Task.Delay(10000));
        if (completedTask != runTask)
        {
            throw new TimeoutException("TestEchoAgent: Agent run timed out");
        }

        while (await reader.WaitToReadAsync())
        {
            while (reader.TryRead(out var evt))
            {
                events.Add(evt);
            }
        }

        // Assert
        Assert.NotEmpty(events);
        var runStarted = events.FirstOrDefault(e => e is RunStartedEvent);
        Assert.NotNull(runStarted);
        var textMessageStart = events.FirstOrDefault(e => e is TextMessageStartEvent);
        Assert.NotNull(textMessageStart);
        var textMessageContent = events.FirstOrDefault(e => e is TextMessageContentEvent);
        Assert.NotNull(textMessageContent);
        var content = (TextMessageContentEvent)textMessageContent;
        Assert.Contains("Echoing user message", content.Delta);
        Assert.Contains("Hello!", content.Delta);
        var textMessageEnd = events.FirstOrDefault(e => e is TextMessageEndEvent);
        Assert.NotNull(textMessageEnd);
        var runFinished = events.FirstOrDefault(e => e is RunFinishedEvent);
        Assert.NotNull(runFinished);
    }

    [Fact]
    public async Task TestChatClientAgent()
    {
        // Arrange
        var mockConfig = new MockChatClientConfig
        {
            ToolCalls = new List<MockToolCall>
            {
                new MockToolCall
                {
                    MessageId = "msg_weather",
                    CallId = "call_weather",
                    Name = "get_weather",
                    Arguments = new Dictionary<string, object?> { { "location", "New York" } }
                }
            }
        };
        var chatClient = new MockChatClient(mockConfig);
        var agent = new ChatClientAgent(chatClient, new ChatClientAgentOptions
        {
            SystemMessage = "You are a helpful assistant.",
            EmitBackendToolCalls = true
        });

        var messages = new List<BaseMessage>
        {
            new SystemMessage
            {
                Id = "sys_1",
                Content = "You are a helpful assistant."
            },
            new UserMessage
            {
                Id = "user_1",
                Content = "What's the weather in New York?"
            }
        };

        var events = new List<BaseEvent>();
        var channel = Channel.CreateUnbounded<BaseEvent>();
        var writer = channel.Writer;
        var reader = channel.Reader;

        // Act
        var runTask = agent.RunAsync(CreateRunAgentInput(messages), writer);
        var completedTask = await Task.WhenAny(runTask, Task.Delay(10000));
        if (completedTask != runTask)
        {
            throw new TimeoutException("TestChatClientAgent: Agent run timed out");
        }

        while (await reader.WaitToReadAsync())
        {
            while (reader.TryRead(out var evt))
            {
                events.Add(evt);
            }
        }

        // Assert
        Assert.NotEmpty(events);
        var toolCallStart = events.FirstOrDefault(e => e is ToolCallStartEvent);
        Assert.NotNull(toolCallStart);
        var start = (ToolCallStartEvent)toolCallStart;
        Assert.Equal("get_weather", start.ToolCallName);
        var toolCallArgs = events.FirstOrDefault(e => e is ToolCallArgsEvent);
        Assert.NotNull(toolCallArgs);
        var args = (ToolCallArgsEvent)toolCallArgs;
        Assert.Contains("New York", args.Delta);
        var toolCallEnd = events.FirstOrDefault(e => e is ToolCallEndEvent);
        Assert.NotNull(toolCallEnd);
    }

    [Fact]
    public async Task TestStatefulChatClientAgent()
    {
        // Arrange
        var mockConfig = new MockChatClientConfig
        {
            SimulateStateRetrieval = true,
            SimulateStateUpdate = true,
            InitialState = new { count = 0 },
            UpdatedState = new { count = 1 }
        };
        var chatClient = new MockChatClient(mockConfig);
        var initialState = new { count = 0 };
        var agent = new StatefulChatClientAgent<object>(chatClient, initialState, new StatefulChatClientAgentOptions<object>
        {
            SystemMessage = "You are a helpful assistant that can count.",
            EmitBackendToolCalls = true,
            EmitStateFunctionsToFrontend = true
        });

        var messages = new List<BaseMessage>
        {
            new SystemMessage
            {
                Id = "sys_1",
                Content = "You are a helpful assistant that can count."
            },
            new UserMessage
            {
                Id = "user_1",
                Content = "Increment the counter"
            }
        };

        var events = new List<BaseEvent>();
        var channel = Channel.CreateUnbounded<BaseEvent>();
        var writer = channel.Writer;
        var reader = channel.Reader;

        // Act
        var runTask = agent.RunAsync(CreateRunAgentInput(messages), writer);
        var completedTask = await Task.WhenAny(runTask, Task.Delay(10000));
        if (completedTask != runTask)
        {
            throw new TimeoutException("TestStatefulChatClientAgent: Agent run timed out");
        }

        while (await reader.WaitToReadAsync())
        {
            while (reader.TryRead(out var evt))
            {
                events.Add(evt);
            }
        }

        // Assert
        Assert.NotEmpty(events);
        
        // Check that we have the basic lifecycle events
        var runStarted = events.FirstOrDefault(e => e is RunStartedEvent);
        Assert.NotNull(runStarted);
        var runFinished = events.FirstOrDefault(e => e is RunFinishedEvent);
        Assert.NotNull(runFinished);
        
        // Check that we have state snapshot event (StatefulChatClientAgent should emit this)
        var stateSnapshot = events.FirstOrDefault(e => e is StateSnapshotEvent);
        Assert.NotNull(stateSnapshot);
        
        // Check that we have tool calls (state functions should be called)
        var toolCallStart = events.FirstOrDefault(e => e is ToolCallStartEvent);
        Assert.NotNull(toolCallStart);
        var toolCallEnd = events.FirstOrDefault(e => e is ToolCallEndEvent);
        Assert.NotNull(toolCallEnd);
    }
}

 