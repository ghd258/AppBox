namespace AppBoxCore;

public readonly struct InvokeArgs
{
    private readonly MessageReadStream? _stream;

    private InvokeArgs(MessageReadStream stream)
    {
        _stream = stream;
    }

    public void Free()
    {
        if (_stream != null)
            MessageReadStream.Return(_stream);
    }

    public static InvokeArgs From(MessageReadStream stream) => new(stream);

    public static InvokeArgs Make<T>(T arg)
    {
        var writer = MessageWriteStream.Rent();
        writer.Serialize(arg);
        var data = writer.FinishWrite();
        MessageWriteStream.Return(writer);

        var reader = MessageReadStream.Rent(data);
        return new InvokeArgs(reader);
    }

    public static InvokeArgs Make<T1, T2>(T1 arg1, T2 arg2) => throw new NotImplementedException();

    public static InvokeArgs Make<T1, T2, T3, T4>(T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        var writer = MessageWriteStream.Rent();
        writer.Serialize(arg1);
        writer.Serialize(arg2);
        writer.Serialize(arg3);
        writer.Serialize(arg4);
        var data = writer.FinishWrite();
        MessageWriteStream.Return(writer);

        var reader = MessageReadStream.Rent(data);
        return new InvokeArgs(reader);
    }

    #region ====GetXXX Methods====

    public int GetInt()
    {
        var payloadType = (PayloadType)_stream!.ReadByte();
        if (payloadType == PayloadType.Int32) return _stream.ReadInt();
        throw new SerializationException(SerializationError.PayloadTypeNotMatch);
    }

    public string? GetString()
    {
        var payloadType = (PayloadType)_stream!.ReadByte();
        if (payloadType == PayloadType.String) return _stream.ReadString();
        if (payloadType == PayloadType.Null) return null;
        throw new SerializationException(SerializationError.PayloadTypeNotMatch);
    }

    #endregion
}