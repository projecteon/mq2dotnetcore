using Microsoft.Extensions.Logging;
using MQ2DotNetCore.Base;
using System;

namespace MQ2DotNetCore.MQ2Api
{
	public class MQ2Dependencies : IDisposable
	{
		protected bool _isDisposed = false;
		protected readonly ILogger<MQ2Dependencies>? _logger;

		protected ChatUtilities? _chat;
		protected MQ2SubmoduleCommandRegistry? _commandRegistry;
		protected MQ2SubmoduleEventRegistry? _eventRegistry;
		protected MQ2? _mq2;
		protected MQ2SynchronizationContext? _mq2SynchronizationContext;
		protected MQ2TypeFactory? _mq2TypeFactory;
		protected MQ2Spawns? _spawns;
		protected MQ2Tlo? _tlo;

		public MQ2Dependencies(
			ChatUtilities chat,
			MQ2SubmoduleCommandRegistry commandRegistry,
			MQ2SubmoduleEventRegistry eventRegistry,
			ILogger<MQ2Dependencies>? logger,
			MQ2 mq2,
			MQ2SynchronizationContext mq2SynchronizationContext,
			MQ2TypeFactory mq2TypeFactory,
			MQ2Spawns spawns,
			string submoduleName,
			MQ2Tlo tlo
		)
		{
			_chat = chat;
			_commandRegistry = commandRegistry;
			_eventRegistry = eventRegistry;
			_logger = logger;
			_mq2 = mq2;
			_mq2SynchronizationContext = mq2SynchronizationContext;
			_mq2TypeFactory = mq2TypeFactory;
			_spawns = spawns;
			_tlo = tlo;

			SubmoduleName = submoduleName;
		}


		public string SubmoduleName { get; }


		public ChatUtilities GetChat()
		{
			CleanupHelper.DisposedCheck(_isDisposed, nameof(MQ2DataType));
			return _chat ?? throw new NullReferenceException($"{nameof(_chat)} field is null");
		}

		public MQ2SubmoduleCommandRegistry GetCommandRegistry()
		{
			CleanupHelper.DisposedCheck(_isDisposed, nameof(MQ2DataType));
			return _commandRegistry ?? throw new NullReferenceException($"{nameof(_commandRegistry)} field is null");
		}

		public MQ2SubmoduleEventRegistry GetEventRegistry()
		{
			CleanupHelper.DisposedCheck(_isDisposed, nameof(MQ2DataType));
			return _eventRegistry ?? throw new NullReferenceException($"{nameof(_eventRegistry)} field is null");
		}

		public MQ2 GetMQ2()
		{
			CleanupHelper.DisposedCheck(_isDisposed, nameof(MQ2DataType));
			return _mq2 ?? throw new NullReferenceException($"{nameof(_mq2)} field is null");
		}

		public MQ2Spawns GetSpawns()
		{
			CleanupHelper.DisposedCheck(_isDisposed, nameof(MQ2DataType));
			return _spawns ?? throw new NullReferenceException($"{nameof(_spawns)} field is null");
		}

		public MQ2SynchronizationContext GetSynchronizationContext()
		{
			CleanupHelper.DisposedCheck(_isDisposed, nameof(MQ2DataType));
			return _mq2SynchronizationContext ?? throw new NullReferenceException($"{nameof(_mq2SynchronizationContext)} field is null");
		}

		public MQ2Tlo GetTlo()
		{
			CleanupHelper.DisposedCheck(_isDisposed, nameof(MQ2DataType));
			return _tlo ?? throw new NullReferenceException($"{nameof(_tlo)} field is null");
		}

		public MQ2TypeFactory GetTypeFactory()
		{
			CleanupHelper.DisposedCheck(_isDisposed, nameof(MQ2DataType));
			return _mq2TypeFactory ?? throw new NullReferenceException($"{nameof(_mq2TypeFactory)} field is null");
		}

		public void Dispose()
		{
			if (_isDisposed)
			{
				return;
			}

			CleanupHelper.TryDispose(_commandRegistry, _logger);
			CleanupHelper.TryDispose(_eventRegistry, _logger);
			CleanupHelper.TryDispose(_mq2TypeFactory, _logger);

			_chat = null;
			_commandRegistry = null;
			_eventRegistry = null;
			_mq2 = null;
			_mq2SynchronizationContext = null;
			_mq2TypeFactory = null;
			_spawns = null;
			_tlo = null;

			_isDisposed = true;
		}
	}
}
