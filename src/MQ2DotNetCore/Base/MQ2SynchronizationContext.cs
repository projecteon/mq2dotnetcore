using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace MQ2DotNetCore.Base
{
	/// <summary>
	/// Per Alynel, the vast majority of EQ/MQ2 calls are not thread safe so we'll start most tasks using the task factory combined with this
	/// synchronization context to ensure the task continuations run on the 'EQ' thread by default.
	/// </summary>
	public class MQ2SynchronizationContext : SynchronizationContext
	{
		private readonly ConcurrentQueue<KeyValuePair<SendOrPostCallback, object?>> _continuationsQueue =
			new ConcurrentQueue<KeyValuePair<SendOrPostCallback, object?>>();

		/// <inheritdoc />
		public override void Post(SendOrPostCallback d, object? state)
		{
			_continuationsQueue.Enqueue(new KeyValuePair<SendOrPostCallback, object?>(d, state));
		}

		/// <inheritdoc />
		public override void Send(SendOrPostCallback d, object? state)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Invoke all queued continuations
		/// </summary>
		/// <param name="setSyncContext">
		/// If true, continuations will be invoked on this synchronization context. If false, they will be invoked on SynchronizationContext.Current
		/// </param>
		public void DoEvents(bool setSyncContext)
		{
			void Action()
			{
				// Any continuations currently in the queue get removed and inserted into a list
				var continuations = new List<KeyValuePair<SendOrPostCallback, object?>>();
				while (_continuationsQueue.TryDequeue(out var continuation))
				{
					continuations.Add(continuation);
				}

				// Now all the continuations in the list get executed
				// Any further continuations posted as a result of one of the existing ones will go in the queue to get executed next iteration of the loop
				foreach (var continuation in continuations)
				{
					continuation.Key(continuation.Value);
				}
			}

			if (setSyncContext)
			{
				SetExecuteAndRestore(Action);
			}
			else
			{
				Action();
			}
		}

		/// <summary>
		/// Helper method to invoke an action on the sync context, restoring the original context after completion or on exception
		/// </summary>
		/// <param name="actionToExecute"></param>
		public void SetExecuteAndRestore(Action actionToExecute)
		{
			if (actionToExecute == null)
			{
				throw new ArgumentNullException(nameof(actionToExecute));
			}

			var oldContext = Current;
			try
			{
				SetSynchronizationContext(this);
				actionToExecute();
			}
			finally
			{
				SetSynchronizationContext(oldContext);
			}
		}

		/// <summary>
		/// Helper method to invoke an action on the sync context, restoring the original context after completion or on exception
		/// </summary>
		public T SetExecuteAndRestore<T>(Func<T> functionToExecute)
		{
			if (functionToExecute == null)
			{
				throw new ArgumentNullException(nameof(functionToExecute));
			}

			var oldContext = Current;
			try
			{
				SetSynchronizationContext(this);
				return functionToExecute();
			}
			finally
			{
				SetSynchronizationContext(oldContext);
			}
		}

		/// <summary>
		/// Remove all queued continuations
		/// </summary>
		/// <returns></returns>
		public int RemoveAllContinuations()
		{
			var count = _continuationsQueue.Count;
			_continuationsQueue.Clear();

			return count;
		}
	}
}
