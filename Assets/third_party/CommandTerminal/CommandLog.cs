using System.Collections.Generic;
using UnityEngine;

namespace CommandTerminal
{
  public enum TerminalLogType
  {
    Error = LogType.Error,
    Assert = LogType.Assert,
    Warning = LogType.Warning,
    Message = LogType.Log,
    Exception = LogType.Exception,
    Input,
    ShellMessage
  }

  public struct LogItem
  {
    public TerminalLogType type;
    public string message;
    public string messageColor;
    public string stack_trace;

    public override string ToString()
    {
      if (messageColor == null)
      {
        return message;
      }
      return "<color=#" + messageColor + ">" + message + "</color>";
    }
  }

  public class CommandLog
  {
    List<LogItem> logs = new List<LogItem>();
    int max_items;

    public event System.Action onLogsAdded;
    public event System.Action<LogItem> onLogsShifted;
    public event System.Action onLogsCleared;

    public List<LogItem> Logs
    {
      get { return logs; }
    }

    public CommandLog(int max_items)
    {
      this.max_items = max_items;
    }

    public void HandleLog(string message, TerminalLogType type, string messageColor)
    {
      HandleLog(message, "", type, messageColor);
    }

    public void HandleLog(string message, string stack_trace, TerminalLogType type, string messageColor)
    {
      LogItem log = new LogItem()
      {
        message = message,
        messageColor = messageColor,
        stack_trace = stack_trace,
        type = type
      };

      logs.Add(log);
      onLogsAdded?.Invoke();

      if (logs.Count > max_items)
      {
        LogItem item = logs[0];
        logs.RemoveAt(0);
        onLogsShifted?.Invoke(item);
      }
    }

    public void Clear()
    {
      logs.Clear();
      onLogsCleared?.Invoke();
    }
  }
}
