using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;

namespace AGUIDotnet.Tests;

public class MockChatClient : IChatClient, IDisposable
{
    private readonly MockChatClientConfig _config;

    public MockChatClient(MockChatClientConfig? config = null)
    {
        _config = config ?? new MockChatClientConfig();
    }

    public async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {   
        await Task.Yield();
        return new ChatResponse();
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        
        // State management functions (for StatefulChatClientAgent)
        if (_config.SimulateStateRetrieval)
        {
            yield return new ChatResponseUpdate
            {
                MessageId = "msg_state_retrieve",
                Contents = new List<AIContent>
                {
                    new FunctionCallContent(
                        callId: "call_retrieve_state",
                        name: "retrieve_state",
                        arguments: new Dictionary<string, object?>()
                    )
                }
            };

            yield return new ChatResponseUpdate
            {
                MessageId = "msg_state_result",
                Contents = new List<AIContent>
                {
                    new FunctionResultContent(
                        callId: "call_retrieve_state",
                        result: JsonSerializer.Serialize(_config.InitialState)
                    )
                }
            };
        }

        if (_config.SimulateStateUpdate)
        {
            yield return new ChatResponseUpdate
            {
                MessageId = "msg_state_update",
                Contents = new List<AIContent>
                {
                    new FunctionCallContent(
                        callId: "call_update_state",
                        name: "update_state",
                        arguments: new Dictionary<string, object?> { { "newState", JsonSerializer.Serialize(_config.UpdatedState) } }
                    )
                }
            };

            yield return new ChatResponseUpdate
            {
                MessageId = "msg_state_update_result",
                Contents = new List<AIContent>
                {
                    new FunctionResultContent(
                        callId: "call_update_state",
                        result: "State updated successfully"
                    )
                }
            };
        }

        // Text response
        if (!string.IsNullOrEmpty(_config.TextResponse))
        {
            yield return new ChatResponseUpdate
            {
                MessageId = _config.TextMessageId,
                Contents = new List<AIContent>
                {
                    new TextContent(_config.TextResponse)
                }
            };
        }

        // Tool calls
        foreach (var toolCall in _config.ToolCalls)
        {
            yield return new ChatResponseUpdate
            {
                MessageId = toolCall.MessageId,
                Contents = new List<AIContent>
                {
                    new FunctionCallContent(
                        callId: toolCall.CallId,
                        name: toolCall.Name,
                        arguments: toolCall.Arguments
                    )
                }
            };
        }

        // Final completion message
        if (!string.IsNullOrEmpty(_config.CompletionMessage))
        {
            yield return new ChatResponseUpdate
            {
                MessageId = "msg_completion",
                Contents = new List<AIContent>
                {
                    new TextContent(_config.CompletionMessage)
                }
            };
        }
    }

    public T GetService<T>(object? key = null) where T : class
    {
        throw new NotImplementedException();
    }

    public object GetService(Type serviceType, object? key = null)
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        // Nothing to dispose
    }
}

public class MockChatClientConfig
{
    public bool SimulateStateRetrieval { get; set; } = false;
    public bool SimulateStateUpdate { get; set; } = false;
    public object InitialState { get; set; } = new { count = 0 };
    public object UpdatedState { get; set; } = new { count = 1 };
    
    public string TextResponse { get; set; } = "I'll help you with that.";
    public string TextMessageId { get; set; } = "msg_text";
    
    public List<MockToolCall> ToolCalls { get; set; } = new();
    
    public string CompletionMessage { get; set; } = "Run finished.";
}

public class MockToolCall
{
    public string MessageId { get; set; } = "msg_tool";
    public string CallId { get; set; } = "call_tool";
    public string Name { get; set; } = "";
    public Dictionary<string, object?> Arguments { get; set; } = new();
} 