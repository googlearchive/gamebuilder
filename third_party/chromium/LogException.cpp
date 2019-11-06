static void LogException(std::string prefix, v8::Isolate *isolate, v8::TryCatch *try_catch)
{
  v8::HandleScope handle_scope(isolate);
  v8::String::Utf8Value exception(isolate, try_catch->Exception());
  const char *exception_string = *exception;
  v8::Local<v8::Message> message = try_catch->Message();
  std::ostringstream sbuf;
  sbuf << prefix;

  if (message.IsEmpty())
  {
    // V8 didn't provide any extra information about this error; just
    // print the exception.
    sbuf << exception_string;
  }
  else
  {
    // Print (filename):(line number): (message).
    v8::String::Utf8Value filename(isolate, message->GetScriptOrigin().ResourceName());
    v8::Local<v8::Context> context(isolate->GetCurrentContext());
    int linenum = message->GetLineNumber(context).FromJust();
    sbuf << *filename << ":" << linenum << ": " << exception_string << std::endl;

    // Print line of source code.
    v8::String::Utf8Value sourceline(isolate,
                                     message->GetSourceLine(context).ToLocalChecked());
    sbuf << "The code:" << std::endl
         << *sourceline << std::endl;
    ;

    // Print wavy underline (GetUnderline is deprecated).
    int start = message->GetStartColumn(context).FromJust();
    for (int i = 0; i < start; i++)
    {
      sbuf << " ";
    }
    int end = message->GetEndColumn(context).FromJust();
    for (int i = start; i < end; i++)
    {
      sbuf << "^";
    }
    sbuf << std::endl;

    sbuf << "Stack trace: " << std::endl;

    v8::Local<v8::Value> stack_trace_string;
    if (try_catch->StackTrace(context).ToLocal(&stack_trace_string) &&
        stack_trace_string->IsString() &&
        v8::Local<v8::String>::Cast(stack_trace_string)->Length() > 0)
    {
      v8::String::Utf8Value stack_trace(isolate, stack_trace_string);
      sbuf << *stack_trace << std::endl;
    }
  }

  LogError(sbuf.str().c_str());
}
