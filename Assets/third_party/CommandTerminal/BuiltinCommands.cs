using System.Text;
using System.Diagnostics;
using UnityEngine;

namespace CommandTerminal
{
  public static class BuiltinCommands
  {
    [RegisterCommand(Help = "Clear the command console", MaxArgCount = 0)]
    static void CommandClear(CommandArg[] args)
    {
      HeadlessTerminal.Buffer.Clear();
    }

    [RegisterCommand(Help = "Display help information about a command", MaxArgCount = 1)]
    static void CommandHelp(CommandArg[] args)
    {
      if (args.Length == 0)
      {
        foreach (var command in HeadlessTerminal.Shell.Commands)
        {
          HeadlessTerminal.Log("{0}: {1}", command.Key.PadRight(16), command.Value.help);
        }
        return;
      }

      string command_name = args[0].String.ToUpper();

      if (!HeadlessTerminal.Shell.Commands.ContainsKey(command_name))
      {
        HeadlessTerminal.Shell.IssueErrorMessage("Command {0} could not be found.", command_name);
        return;
      }

      var info = HeadlessTerminal.Shell.Commands[command_name];

      if (info.help == null)
      {
        HeadlessTerminal.Log("{0} does not provide any help documentation.", command_name);
      }
      else if (info.hint == null)
      {
        HeadlessTerminal.Log(info.help);
      }
      else
      {
        HeadlessTerminal.Log("{0}\nUsage: {1}", info.help, info.hint);
      }
    }

    [RegisterCommand(Help = "Time the execution of a command", MinArgCount = 1)]
    static void CommandTime(CommandArg[] args)
    {
      var sw = new Stopwatch();
      sw.Start();

      HeadlessTerminal.Shell.RunCommand(JoinArguments(args));

      sw.Stop();
      HeadlessTerminal.Log("Time: {0}ms", (double)sw.ElapsedTicks / 10000);
    }

    [RegisterCommand(Help = "Output message")]
    static void CommandPrint(CommandArg[] args)
    {
      HeadlessTerminal.Log(JoinArguments(args));
    }

#if DEBUG
    [RegisterCommand(Help = "Output the stack trace of the previous message", MaxArgCount = 0)]
    static void CommandTrace(CommandArg[] args)
    {
      int log_count = HeadlessTerminal.Buffer.Logs.Count;

      if (log_count - 2 < 0)
      {
        HeadlessTerminal.Log("Nothing to trace.");
        return;
      }

      var log_item = HeadlessTerminal.Buffer.Logs[log_count - 2];

      if (log_item.stack_trace == "")
      {
        HeadlessTerminal.Log("{0} (no trace)", log_item.message);
      }
      else
      {
        HeadlessTerminal.Log(log_item.stack_trace);
      }
    }
#endif

    [RegisterCommand(Help = "List all variables or set a variable value")]
    static void CommandSet(CommandArg[] args)
    {
      if (args.Length == 0)
      {
        foreach (var kv in HeadlessTerminal.Shell.Variables)
        {
          HeadlessTerminal.Log("{0}: {1}", kv.Key.PadRight(16), kv.Value);
        }
        return;
      }

      string variable_name = args[0].String;

      if (variable_name[0] == '$')
      {
        HeadlessTerminal.Log(TerminalLogType.Warning, "Warning: Variable name starts with '$', '${0}'.", variable_name);
      }

      HeadlessTerminal.Shell.SetVariable(variable_name, JoinArguments(args, 1));
    }

    [RegisterCommand(Help = "No operation")]
    static void CommandNoop(CommandArg[] args) { }

    [RegisterCommand(Help = "Quit running application", MaxArgCount = 0)]
    static void CommandQuit(CommandArg[] args)
    {
#if UNITY_EDITOR
      UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }

    static string JoinArguments(CommandArg[] args, int start = 0)
    {
      var sb = new StringBuilder();
      int arg_length = args.Length;

      for (int i = start; i < arg_length; i++)
      {
        sb.Append(args[i].String);

        if (i < arg_length - 1)
        {
          sb.Append(" ");
        }
      }

      return sb.ToString();
    }
  }
}
