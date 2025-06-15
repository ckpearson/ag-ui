using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Immutable;
using AGUIDotnet.Events;
using Xunit;

namespace AGUIDotnet.Tests;

public class EventTests
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public void TestTextMessageEvents()
    {
        // Test TextMessageStartEvent
        var startEvent = new TextMessageStartEvent
        {
            MessageId = "msg_123",
            Timestamp = 1648214400000
        };

        var startJson = JsonSerializer.Serialize(startEvent, _jsonOptions);
        var deserializedStart = JsonSerializer.Deserialize<TextMessageStartEvent>(startJson, _jsonOptions);

        Assert.NotNull(deserializedStart);
        Assert.Equal("msg_123", deserializedStart.MessageId);

        // Test TextMessageContentEvent
        var contentEvent = new TextMessageContentEvent
        {
            MessageId = "msg_123",
            Delta = "Hello, world!",
            Timestamp = 1648214400000
        };

        var contentJson = JsonSerializer.Serialize(contentEvent, _jsonOptions);
        var deserializedContent = JsonSerializer.Deserialize<TextMessageContentEvent>(contentJson, _jsonOptions);

        Assert.NotNull(deserializedContent);
        Assert.Equal("msg_123", deserializedContent.MessageId);
        Assert.Equal("Hello, world!", deserializedContent.Delta);

        // Test TextMessageEndEvent
        var endEvent = new TextMessageEndEvent
        {
            MessageId = "msg_123",
            Timestamp = 1648214400000
        };

        var endJson = JsonSerializer.Serialize(endEvent, _jsonOptions);
        var deserializedEnd = JsonSerializer.Deserialize<TextMessageEndEvent>(endJson, _jsonOptions);

        Assert.NotNull(deserializedEnd);
        Assert.Equal("msg_123", deserializedEnd.MessageId);
    }

    [Fact]
    public void TestToolCallEvents()
    {
        // Test ToolCallStartEvent
        var startEvent = new ToolCallStartEvent
        {
            ToolCallId = "call_123",
            ToolCallName = "get_weather",
            ParentMessageId = "msg_456",
            Timestamp = 1648214400000
        };

        var startJson = JsonSerializer.Serialize(startEvent, _jsonOptions);
        var deserializedStart = JsonSerializer.Deserialize<ToolCallStartEvent>(startJson, _jsonOptions);

        Assert.NotNull(deserializedStart);
        Assert.Equal("call_123", deserializedStart.ToolCallId);
        Assert.Equal("get_weather", deserializedStart.ToolCallName);
        Assert.Equal("msg_456", deserializedStart.ParentMessageId);

        // Test ToolCallArgsEvent
        var argsEvent = new ToolCallArgsEvent
        {
            ToolCallId = "call_123",
            Delta = "{\"location\":\"New York\"}",
            Timestamp = 1648214400000
        };

        var argsJson = JsonSerializer.Serialize(argsEvent, _jsonOptions);
        var deserializedArgs = JsonSerializer.Deserialize<ToolCallArgsEvent>(argsJson, _jsonOptions);

        Assert.NotNull(deserializedArgs);
        Assert.Equal("call_123", deserializedArgs.ToolCallId);
        Assert.Equal("{\"location\":\"New York\"}", deserializedArgs.Delta);

        // Test ToolCallEndEvent
        var endEvent = new ToolCallEndEvent
        {
            ToolCallId = "call_123",
            Timestamp = 1648214400000
        };

        var endJson = JsonSerializer.Serialize(endEvent, _jsonOptions);
        var deserializedEnd = JsonSerializer.Deserialize<ToolCallEndEvent>(endJson, _jsonOptions);

        Assert.NotNull(deserializedEnd);
        Assert.Equal("call_123", deserializedEnd.ToolCallId);
    }

    [Fact]
    public void TestStateEvents()
    {
        // Test StateSnapshotEvent
        var snapshot = new Dictionary<string, object>
        {
            ["conversation_state"] = "active",
            ["user_info"] = new Dictionary<string, object>
            {
                ["name"] = "John"
            }
        };

        var snapshotEvent = new StateSnapshotEvent
        {
            Snapshot = snapshot,
            Timestamp = 1648214400000
        };

        var snapshotJson = JsonSerializer.Serialize(snapshotEvent, _jsonOptions);
        var deserializedSnapshot = JsonSerializer.Deserialize<StateSnapshotEvent>(snapshotJson, _jsonOptions);

        Assert.NotNull(deserializedSnapshot);
        Assert.NotNull(deserializedSnapshot.Snapshot);
        var snapshotDict = JsonSerializer.Deserialize<Dictionary<string, object>>(deserializedSnapshot.Snapshot.ToString()!);
        Assert.NotNull(snapshotDict);
        Assert.Equal("active", snapshotDict["conversation_state"].ToString());

        // Test StateDeltaEvent
        var delta = ImmutableList.Create<object>(
            new { op = "replace", path = "/conversation_state", value = "paused" },
            new { op = "add", path = "/user_info/age", value = 30 }
        );

        var deltaEvent = new StateDeltaEvent
        {
            Delta = delta,
            Timestamp = 1648214400000
        };

        var deltaJson = JsonSerializer.Serialize(deltaEvent, _jsonOptions);
        var deserializedDelta = JsonSerializer.Deserialize<StateDeltaEvent>(deltaJson, _jsonOptions);

        Assert.NotNull(deserializedDelta);
        Assert.Equal(2, deserializedDelta.Delta.Count);
    }

    [Fact]
    public void TestRunEvents()
    {
        // Test RunStartedEvent
        var startedEvent = new RunStartedEvent
        {
            ThreadId = "thread_123",
            RunId = "run_123",
            Timestamp = 1648214400000
        };

        var startedJson = JsonSerializer.Serialize(startedEvent, _jsonOptions);
        var deserializedStarted = JsonSerializer.Deserialize<RunStartedEvent>(startedJson, _jsonOptions);

        Assert.NotNull(deserializedStarted);
        Assert.Equal("run_123", deserializedStarted.RunId);
        Assert.Equal("thread_123", deserializedStarted.ThreadId);

        // Test RunFinishedEvent
        var finishedEvent = new RunFinishedEvent
        {
            ThreadId = "thread_123",
            RunId = "run_123",
            Timestamp = 1648214400000
        };

        var finishedJson = JsonSerializer.Serialize(finishedEvent, _jsonOptions);
        var deserializedFinished = JsonSerializer.Deserialize<RunFinishedEvent>(finishedJson, _jsonOptions);

        Assert.NotNull(deserializedFinished);
        Assert.Equal("run_123", deserializedFinished.RunId);
        Assert.Equal("thread_123", deserializedFinished.ThreadId);

        // Test RunErrorEvent
        var errorEvent = new RunErrorEvent
        {
            Message = "Something went wrong",
            Code = "ERROR_001",
            Timestamp = 1648214400000
        };

        var errorJson = JsonSerializer.Serialize(errorEvent, _jsonOptions);
        var deserializedError = JsonSerializer.Deserialize<RunErrorEvent>(errorJson, _jsonOptions);

        Assert.NotNull(deserializedError);
        Assert.Equal("Something went wrong", deserializedError.Message);
        Assert.Equal("ERROR_001", deserializedError.Code);
    }
} 