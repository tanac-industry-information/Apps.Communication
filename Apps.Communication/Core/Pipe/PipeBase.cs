using System;

namespace Apps.Communication.Core.Pipe
{
	/// <summary>
	/// 管道的基础类对象
	/// </summary>
	public class PipeBase : IDisposable
	{
		private SimpleHybirdLock hybirdLock;

		/// <summary>
		/// 实例化一个默认的对象
		/// </summary>
		public PipeBase()
		{
			hybirdLock = new SimpleHybirdLock();
		}

		/// <summary>
		/// 进入管道的锁<br />
		/// Lock into the pipeline
		/// </summary>
		public void PipeLockEnter()
		{
			hybirdLock.Enter();
		}

		/// <summary>
		/// 离开管道的锁<br />
		/// Lock out of the pipeline
		/// </summary>
		public void PipeLockLeave()
		{
			hybirdLock.Leave();
		}

		/// <inheritdoc cref="M:System.IDisposable.Dispose" />
		public virtual void Dispose()
		{
			hybirdLock?.Dispose();
		}
	}
}
