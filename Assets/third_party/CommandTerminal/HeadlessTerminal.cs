using UnityEngine;
using System.Text;
using System.Collections;
using UnityEngine.Assertions;

namespace CommandTerminal
{

  public class HeadlessTerminal : MonoBehaviour
  {
    [SerializeField] int BufferSize = 512;

    static Color ForegroundColor = new Color32(251, 249, 217, 255);
    static Color ShellColor = new Color32(251, 249, 217, 255);
    static Color InputColor = new Color32(149, 216, 232, 255);
    static Color WarningColor = new Color32(249, 220, 47, 255);
    static Color ErrorColor = new Color32(232, 30, 37, 255);

    TerminalState state;
    public string command_text;


    public static CommandLog Buffer { get; private set; }
    public static CommandShell Shell { get; private set; }
    public static CommandHistory History { get; private set; }
    public static CommandAutocomplete Autocomplete { get; private set; }

    public static bool IssuedError
    {
      get { return Shell.IssuedErrorMessage != null; }
    }

    public static void Log(string format, params object[] message)
    {
      Log(TerminalLogType.ShellMessage, format, message);
    }

    public static void Log(TerminalLogType type, string format, params object[] message)
    {
      Buffer.HandleLog(string.Format(format, message), type, ColorUtility.ToHtmlStringRGB(GetLogColor(type)));
    }

    public void Awake()
    {
      Buffer = new CommandLog(BufferSize);
      Shell = new CommandShell();
      History = new CommandHistory();
      Autocomplete = new CommandAutocomplete();

      command_text = "";

      Shell.RegisterCommands();

      if (IssuedError)
      {
        Log(TerminalLogType.Error, "Error: {0}", Shell.IssuedErrorMessage);
      }

      foreach (var command in Shell.Commands)
      {
        Autocomplete.Register(command.Key);
      }
    }

    public void EnterCommand()
    {
      Log(TerminalLogType.Input, "{0}", command_text);
      Shell.RunCommand(command_text);
      History.Push(command_text);

      if (IssuedError)
      {
        Log(TerminalLogType.Error, "Error: {0}", Shell.IssuedErrorMessage);
      }

      command_text = "";
    }

    public void CompleteCommand()
    {
      string head_text = command_text;
      int format_width = 0;

      string[] completion_buffer = Autocomplete.Complete(ref head_text, ref format_width);
      int completion_length = completion_buffer.Length;

      if (completion_length != 0)
      {
        command_text = head_text;
      }

      if (completion_length >= 1)
      {
        // Print possible completions
        var log_buffer = new StringBuilder();

        for (int i = 0; i < completion_buffer.Length; i++)
        {
          if (i == completion_buffer.Length - 1)
          {
            log_buffer.Append(completion_buffer[i]);
          }
          // 9 commands to a line
          else if ((i + 1) % 9 == 0)
          {
            log_buffer.Append(completion_buffer[i] + "\n");
          }
          else
          {
            log_buffer.Append(completion_buffer[i].PadRight(format_width + 4));
          }
        }

        Log("{0}", log_buffer);
      }
    }

    void HandleUnityLog(string message, string stack_trace, LogType type)
    {
      Buffer.HandleLog(message, stack_trace, (TerminalLogType)type, ColorUtility.ToHtmlStringRGB(GetLogColor((TerminalLogType)type)));
    }

    public void UpdateCommandText(string s)
    {
      command_text = s;
    }


    public void HistoryUp()
    {
      command_text = History.Previous();
    }

    public void HistoryDown()
    {
      command_text = History.Next();
    }

    public static Color GetLogColor(TerminalLogType type)
    {
      switch (type)
      {
        case TerminalLogType.Message: return ForegroundColor;
        case TerminalLogType.Warning: return WarningColor;
        case TerminalLogType.Input: return InputColor;
        case TerminalLogType.ShellMessage: return ShellColor;
        default: return ErrorColor;
      }
    }
  }
}
