using System.Text.Json;
using System.Collections.Immutable;
using AGUIDotnet.Types;
using Xunit;

namespace AGUIDotnet.Tests;

public class TypeTests
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    [Fact]
    public void TestUserMessageSerialization()
    {
        // Arrange
        var message = new UserMessage
        {
            Id = "msg_123",
            Content = "Hello, world!"
        };

        // Act
        var json = JsonSerializer.Serialize(message, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<UserMessage>(json, _jsonOptions);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal("msg_123", deserialized.Id);
        Assert.Equal("Hello, world!", deserialized.Content);
    }

    [Fact]
    public void TestAssistantMessageWithToolCalls()
    {
        // Arrange
        var message = new AssistantMessage
        {
            Id = "msg_456",
            Content = "I'll help you with that",
            ToolCalls = ImmutableList.Create(new ToolCall
            {
                Id = "call_789",
                Function = new FunctionCall
                {
                    Name = "get_weather",
                    Arguments = "{\"location\":\"New York\"}"
                }
            })
        };

        // Act
        var json = JsonSerializer.Serialize(message, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<AssistantMessage>(json, _jsonOptions);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal("msg_456", deserialized.Id);
        Assert.Equal("I'll help you with that", deserialized.Content);
        Assert.Single(deserialized.ToolCalls);
        Assert.Equal("call_789", deserialized.ToolCalls[0].Id);
        Assert.Equal("get_weather", deserialized.ToolCalls[0].Function.Name);
    }

    [Fact]
    public void TestToolMessage()
    {
        // Arrange
        var message = new ToolMessage
        {
            Id = "tool_123",
            Content = "Weather data for New York",
            ToolCallId = "call_789"
        };

        // Act
        var json = JsonSerializer.Serialize(message, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<ToolMessage>(json, _jsonOptions);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal("tool_123", deserialized.Id);
        Assert.Equal("Weather data for New York", deserialized.Content);
        Assert.Equal("call_789", deserialized.ToolCallId);
    }

    [Fact]
    public void TestSystemMessage()
    {
        // Arrange
        var message = new SystemMessage
        {
            Id = "sys_123",
            Content = "You are a helpful assistant."
        };

        // Act
        var json = JsonSerializer.Serialize(message, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<SystemMessage>(json, _jsonOptions);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal("sys_123", deserialized.Id);
        Assert.Equal("You are a helpful assistant.", deserialized.Content);
    }

    [Fact]
    public void TestDeveloperMessage()
    {
        // Arrange
        var message = new DeveloperMessage
        {
            Id = "dev_123",
            Content = "Debug information"
        };

        // Act
        var json = JsonSerializer.Serialize(message, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<DeveloperMessage>(json, _jsonOptions);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal("dev_123", deserialized.Id);
        Assert.Equal("Debug information", deserialized.Content);
    }
} 