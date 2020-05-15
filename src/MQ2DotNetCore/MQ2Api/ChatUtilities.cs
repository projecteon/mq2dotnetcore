using JetBrains.Annotations;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MQ2DotNetCore.MQ2Api
{
	/// <summary>
	/// Contains utility methods and properties relating to ingame chat (messages in a chat window, from EQ or MQ2)
	/// </summary>
	[PublicAPI]
	public class ChatUtilities
	{
		private readonly MQ2SubmoduleEventRegistry _submoduleEventRegistry;

		internal ChatUtilities(MQ2SubmoduleEventRegistry submoduleEventRegistry)
		{
			_submoduleEventRegistry = submoduleEventRegistry;
		}

		/// <summary>
		/// Wait indefinitely for a line of chat from either EQ or MQ2 matching <paramref name="predicate"/>
		/// </summary>
		/// <param name="predicate">Function that returns true if a line matches</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the async operation.</param>
		public async Task WaitFor(Predicate<string> predicate, CancellationToken cancellationToken)
		{
			await WaitForInternal(
				predicate,
				(chatEventHandler) => _submoduleEventRegistry.OnChatAny += chatEventHandler,
				(chatEventHandler) => _submoduleEventRegistry.OnChatAny -= chatEventHandler,
				cancellationToken
			);
		}

		/// <summary>
		/// Wait for up to <paramref name="timeout"/> milliseconds for a line of chat from either EQ or MQ2 matching <paramref name="predicate"/>
		/// </summary>
		/// <param name="predicate">Function that returns true if a line matches</param>
		/// <param name="timeout">Number of milliseconds to wait before timing out</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the async operation.</param>
		/// <returns>Returns true if a match was found or false if the timeout elapsed</returns>
		/// <exception cref="TaskCanceledException" />
		public async Task<bool> WaitFor(Predicate<string> predicate, int timeout, CancellationToken cancellationToken)
		{
			return await WaitForInternalWithTimeout(
				predicate,
				timeout,
				(chatEventHandler) => _submoduleEventRegistry.OnChatAny += chatEventHandler,
				(chatEventHandler) => _submoduleEventRegistry.OnChatAny -= chatEventHandler,
				cancellationToken
			);
		}

		/// <summary>
		/// Wait indefinitely for a line of chat from EQ (and not MQ2) matching <paramref name="predicate"/>
		/// </summary>
		/// <param name="predicate">Function that returns true if a line matches</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the async operation.</param>
		public async Task WaitForEQ(Predicate<string> predicate, CancellationToken cancellationToken)
		{
			await WaitForInternal(
				predicate,
				(chatEventHandler) => _submoduleEventRegistry.OnChatEQ += chatEventHandler,
				(chatEventHandler) => _submoduleEventRegistry.OnChatEQ -= chatEventHandler,
				cancellationToken
			);
		}

		/// <summary>
		/// Wait for up to <paramref name="timeout"/> milliseconds for a line of chat from EQ (and not MQ2) matching <paramref name="predicate"/>
		/// </summary>
		/// <param name="predicate">Function that returns true if a line matches</param>
		/// <param name="timeout">Number of milliseconds to wait before timing out</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the async operation.</param>
		/// <returns>Returns true if a match was found or false if the timeout elapsed</returns>
		/// <exception cref="TaskCanceledException" />
		public async Task<bool> WaitForEQ(Predicate<string> predicate, int timeout, CancellationToken cancellationToken)
		{
			return await WaitForInternalWithTimeout(
				predicate,
				timeout,
				(chatEventHandler) => _submoduleEventRegistry.OnChatEQ += chatEventHandler,
				(chatEventHandler) => _submoduleEventRegistry.OnChatEQ -= chatEventHandler,
				cancellationToken
			);
		}

		/// <summary>
		/// Wait indefinitely for a line of chat from MQ2 (and not EQ) matching <paramref name="predicate"/>
		/// </summary>
		/// <param name="predicate">Function that returns true if a line matches</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the async operation.</param>
		public async Task WaitForMQ2(Predicate<string> predicate, CancellationToken cancellationToken)
		{
			await WaitForInternal(
				predicate,
				(chatEventHandler) => _submoduleEventRegistry.OnChatMQ2 += chatEventHandler,
				(chatEventHandler) => _submoduleEventRegistry.OnChatMQ2 -= chatEventHandler,
				cancellationToken
			);
		}

		/// <summary>
		/// Wait for up to <paramref name="timeout"/> milliseconds for a line of chat from MQ2 (and not EQ) matching <paramref name="predicate"/>
		/// </summary>
		/// <param name="predicate">Function that returns true if a line matches</param>
		/// <param name="timeout">Number of milliseconds to wait before timing out</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the async operation.</param>
		/// <returns>Returns true if a match was found or false if the timeout elapsed</returns>
		/// <exception cref="TaskCanceledException" />
		public async Task<bool> WaitForMQ2(Predicate<string> predicate, int timeout, CancellationToken cancellationToken)
		{
			return await WaitForInternalWithTimeout(
				predicate,
				timeout,
				(chatEventHandler) => _submoduleEventRegistry.OnChatMQ2 += chatEventHandler,
				(chatEventHandler) => _submoduleEventRegistry.OnChatMQ2 -= chatEventHandler,
				cancellationToken
			);
		}

		private async Task WaitForInternal(
			Predicate<string> predicate,
			Action<EventHandler<ChatLineEventArgs>> subscribe,
			Action<EventHandler<ChatLineEventArgs>> unsubscribe,
			CancellationToken cancellationToken
		)
		{
			// Since all the WaitFor* methods are the same, just using a different event, this reduces the need for a lot of boilerplate code
			var found = false;
			void OnChat(object? _, ChatLineEventArgs chatLineEventArgs) {
				if (predicate(chatLineEventArgs.ChatLine))
				{
					found = true;
				}
			}

			subscribe(OnChat);
			try
			{
				while (!found)
				{
					cancellationToken.ThrowIfCancellationRequested();
					await Task.Yield();
				}
			}
			finally
			{
				unsubscribe(OnChat);
			}
		}

		private async Task<bool> WaitForInternalWithTimeout(
			Predicate<string> predicate,
			int timeoutInMilliseconds,
			Action<EventHandler<ChatLineEventArgs>> subscribe,
			Action<EventHandler<ChatLineEventArgs>> unsubscribe,
			CancellationToken cancellationToken
		)
		{
			using (var timeoutCancellationSource = new CancellationTokenSource(timeoutInMilliseconds))
			{
				if (cancellationToken == CancellationToken.None)
				{
					try
					{
						await WaitForInternal(
							predicate,
							subscribe,
							unsubscribe,
							timeoutCancellationSource.Token
						);

						return true;
					}
					catch (TaskCanceledException taskCancelledException)
					{
						if (taskCancelledException?.CancellationToken == timeoutCancellationSource.Token)
						{
							return false;
						}

						throw;
					}
					catch (OperationCanceledException operationCancelledException)
					{
						if (operationCancelledException?.CancellationToken == timeoutCancellationSource.Token)
						{
							return false;
						}

						throw;
					}
				}

				using (var linkedCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(timeoutCancellationSource.Token, cancellationToken))
				{
					try
					{
						await WaitForInternal(
							predicate,
							subscribe,
							unsubscribe,
							linkedCancellationSource.Token
						);

						return true;
					}
					catch (TaskCanceledException taskCancelledException)
					{
						if (taskCancelledException?.CancellationToken == linkedCancellationSource.Token
							&& timeoutCancellationSource.IsCancellationRequested
							&& !cancellationToken.IsCancellationRequested)
						{
							return false;
						}

						throw;
					}
					catch (OperationCanceledException operationCancelledException)
					{
						if (operationCancelledException?.CancellationToken == linkedCancellationSource.Token
							&& timeoutCancellationSource.IsCancellationRequested
							&& !cancellationToken.IsCancellationRequested)
						{
							return false;
						}

						throw;
					}
				}
			}
		}
	}
}
