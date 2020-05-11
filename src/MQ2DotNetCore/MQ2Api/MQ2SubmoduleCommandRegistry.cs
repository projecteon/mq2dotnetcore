using MQ2DotNetCore.Base;
using System;

namespace MQ2DotNetCore.MQ2Api
{
	public sealed class MQ2SubmoduleCommandRegistry : IDisposable
	{
		private readonly MQ2CommandRegistry _baseCommandRegistry;
		private bool _isDisposed = false;
		private readonly string _submoduleName;

		internal MQ2SubmoduleCommandRegistry(MQ2CommandRegistry baseCommandRegistry, string submoduleName)
		{
			_baseCommandRegistry = baseCommandRegistry ?? throw new ArgumentNullException(nameof(baseCommandRegistry));
			_submoduleName = submoduleName;
		}
		public void Dispose()
		{
			try
			{
				_baseCommandRegistry.RemoveCommandsForSubmodule(_submoduleName);
			}
			catch (Exception)
			{

			}

			_isDisposed = true;
		}

		public void AddAsyncCommand(string commandName, MQ2CommandRegistry.AsyncCommand asyncCommandHandler, bool eq = false, bool parse = true, bool inGame = false)
		{
			if (_isDisposed)
			{
				throw new ObjectDisposedException(nameof(MQ2CommandRegistry));
			}

			_baseCommandRegistry.AddAsyncCommand(_submoduleName, commandName, asyncCommandHandler, eq, parse, inGame);
		}

		public void AddCommand(string commandName, MQ2CommandRegistry.Command commandHandler, bool eq = false, bool parse = true, bool inGame = false)
		{
			if (_isDisposed)
			{
				throw new ObjectDisposedException(nameof(MQ2CommandRegistry));
			}

			_baseCommandRegistry.AddCommand(_submoduleName, commandName, commandHandler, eq, parse, inGame);
		}

		public bool TryRemoveCommand(string commandName)
		{
			if (_isDisposed)
			{
				throw new ObjectDisposedException(nameof(MQ2CommandRegistry));
			}

			return _baseCommandRegistry.TryRemoveCommand(commandName, _submoduleName);
		}
	}
}
