namespace AppBoxCore;

/// <summary>
/// 运行时上下文，用于提供模型容器及服务调用
/// </summary>
public interface IRuntimeContext
{
    /// <summary>
    /// 当前用户的会话信息
    /// </summary>
    IUserSession? CurrentSession { get; }

    /// <summary>
    /// 调用服务
    /// </summary>
    /// <param name="service">eg: sys.HelloService.SayHello</param>
    /// <param name="args">注意: 使用完后必须调用args.Free()释放缓存</param>
    ValueTask<AnyValue> InvokeAsync(string service, InvokeArgs args);
}