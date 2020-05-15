using MQ2DotNetCore.Base;
using MQ2DotNetCore.MQ2Api.DataTypes;
using System;

namespace MQ2DotNetCore.MQ2Api
{
#pragma warning disable CA1030 // Use events where appropriate
#pragma warning disable IDE1006 // Naming Styles
	public sealed class MQ2SubmoduleEventRegistry : IDisposable
	{
		private bool _isDisposed = false;
		private readonly string _submoduleName;

		internal MQ2SubmoduleEventRegistry(string submoduleName)
		{
			_submoduleName = submoduleName;
		}

		/// <inheritdoc />
		public void Dispose()
		{
			_isDisposed = true;
		}


		private event EventHandler<GroundType>? _onAddGroundItem;

		/// <summary>
		/// Fired when a new ground item is added. Will be fired once for each ground item in the zone when entering a new zone
		/// </summary>
		public event EventHandler<GroundType>? OnAddGroundItem
		{
			add
			{
				CleanupHelper.DisposedCheck(_isDisposed, nameof(MQ2SubmoduleEventRegistry));
				_onAddGroundItem += value;
			}
			remove
			{
				CleanupHelper.DisposedCheck(_isDisposed, nameof(MQ2SubmoduleEventRegistry));
				_onAddGroundItem -= value;
			}
		}



		private event EventHandler<SpawnType>? _onAddSpawn;

		/// <summary>
		/// Fired when a new spawn is added. Will be fired once for each spawn in the zone when entering a new zone
		/// </summary>
		public event EventHandler<SpawnType>? OnAddSpawn
		{
			add
			{
				CleanupHelper.DisposedCheck(_isDisposed, nameof(MQ2SubmoduleEventRegistry));
				_onAddSpawn += value;
			}
			remove
			{
				CleanupHelper.DisposedCheck(_isDisposed, nameof(MQ2SubmoduleEventRegistry));
				_onAddSpawn -= value;
			}
		}



		private event EventHandler? _onBeginZone;

		/// <summary>
		/// This is called when we receive the EQ_BEGIN_ZONE packet
		/// </summary>
		public event EventHandler? OnBeginZone
		{
			add
			{
				CleanupHelper.DisposedCheck(_isDisposed, nameof(MQ2SubmoduleEventRegistry));
				_onBeginZone += value;
			}
			remove
			{
				CleanupHelper.DisposedCheck(_isDisposed, nameof(MQ2SubmoduleEventRegistry));
				_onBeginZone -= value;
			}
		}



		private event EventHandler<ChatLineEventArgs>? _onChatAny;

		/// <summary>
		/// Fired from a line of chat from either EQ or MQ2
		/// </summary>
		public event EventHandler<ChatLineEventArgs>? OnChatAny
		{
			add
			{
				CleanupHelper.DisposedCheck(_isDisposed, nameof(MQ2SubmoduleEventRegistry));
				_onChatAny += value;
			}
			remove
			{
				CleanupHelper.DisposedCheck(_isDisposed, nameof(MQ2SubmoduleEventRegistry));
				_onChatAny -= value;
			}
		}



		private event EventHandler<ChatLineEventArgs>? _onChatEQ;

		/// <summary>
		/// Fired on a line of chat from EQ
		/// </summary>
		public event EventHandler<ChatLineEventArgs>? OnChatEQ
		{
			add
			{
				CleanupHelper.DisposedCheck(_isDisposed, nameof(MQ2SubmoduleEventRegistry));
				_onChatEQ += value;
			}
			remove
			{
				CleanupHelper.DisposedCheck(_isDisposed, nameof(MQ2SubmoduleEventRegistry));

				_onChatEQ -= value;
			}
		}


		private event EventHandler<ChatLineEventArgs>? _onChatMQ2;

		/// <summary>
		/// Fired on a line of chat from MQ2
		/// </summary>
		public event EventHandler<ChatLineEventArgs>? OnChatMQ2
		{
			add
			{
				CleanupHelper.DisposedCheck(_isDisposed, nameof(MQ2SubmoduleEventRegistry));
				_onChatMQ2 += value;
			}
			remove
			{
				CleanupHelper.DisposedCheck(_isDisposed, nameof(MQ2SubmoduleEventRegistry));
				_onChatMQ2 -= value;
			}
		}



		private event EventHandler? _onCleanUI;

		/// <summary>
		/// Called once directly before shutdown of the new ui system, and also every time the game calls CDisplay::CleanGameUI()
		/// </summary>
		public event EventHandler? OnCleanUI
		{
			add
			{
				CleanupHelper.DisposedCheck(_isDisposed, nameof(MQ2SubmoduleEventRegistry));
				_onCleanUI += value;
			}
			remove
			{
				CleanupHelper.DisposedCheck(_isDisposed, nameof(MQ2SubmoduleEventRegistry));
				_onCleanUI -= value;
			}
		}



		private event EventHandler? _onDrawHUD;

		/// <summary>
		/// Called every frame that the "HUD" is drawn -- e.g. net status / packet loss bar
		/// </summary>
		public event EventHandler? OnDrawHUD
		{
			add
			{
				CleanupHelper.DisposedCheck(_isDisposed, nameof(MQ2SubmoduleEventRegistry));
				_onDrawHUD += value;
			}
			remove
			{
				CleanupHelper.DisposedCheck(_isDisposed, nameof(MQ2SubmoduleEventRegistry));
				_onDrawHUD -= value;
			}
		}


		private event EventHandler? _onEndZone;

		/// <summary>
		/// This is called when we receive the EQ_END_ZONE packet
		/// </summary>
		public event EventHandler? OnEndZone
		{
			add
			{
				CleanupHelper.DisposedCheck(_isDisposed, nameof(MQ2SubmoduleEventRegistry));
				_onEndZone += value;
			}
			remove
			{
				CleanupHelper.DisposedCheck(_isDisposed, nameof(MQ2SubmoduleEventRegistry));
				_onEndZone -= value;
			}
		}



		private event EventHandler? _onReloadUI;

		/// <summary>
		/// Called once directly after the game ui is reloaded, after issuing /loadskin
		/// </summary>
		public event EventHandler? OnReloadUI
		{
			add
			{
				CleanupHelper.DisposedCheck(_isDisposed, nameof(MQ2SubmoduleEventRegistry));
				_onReloadUI += value;
			}
			remove
			{
				CleanupHelper.DisposedCheck(_isDisposed, nameof(MQ2SubmoduleEventRegistry));
				_onReloadUI -= value;
			}
		}


		private event EventHandler<GroundType>? _onRemoveGroundItem;

		/// <summary>
		/// Fired when a ground item is removed. Will be fired once for each ground item in the zone when exiting a zone
		/// </summary>
		public event EventHandler<GroundType>? OnRemoveGroundItem
		{
			add
			{
				CleanupHelper.DisposedCheck(_isDisposed, nameof(MQ2SubmoduleEventRegistry));
				_onRemoveGroundItem += value;
			}
			remove
			{
				CleanupHelper.DisposedCheck(_isDisposed, nameof(MQ2SubmoduleEventRegistry));
				_onRemoveGroundItem -= value;
			}
		}



		private event EventHandler<SpawnType>? _onRemoveSpawn;

		/// <summary>
		/// Fired when a spawn is removed. Will be fired once for each spawn in the zone when exiting a zone
		/// </summary>
		public event EventHandler<SpawnType>? OnRemoveSpawn
		{
			add
			{
				CleanupHelper.DisposedCheck(_isDisposed, nameof(MQ2SubmoduleEventRegistry));
				_onRemoveSpawn += value;
			}
			remove
			{
				CleanupHelper.DisposedCheck(_isDisposed, nameof(MQ2SubmoduleEventRegistry));
				_onRemoveSpawn -= value;
			}
		}



		private event EventHandler<GameState>? _onSetGameState;

		/// <summary>
		/// Called once directly after initialization, and then every time the gamestate changes
		/// </summary>
		public event EventHandler<GameState>? OnSetGameState
		{
			add
			{
				CleanupHelper.DisposedCheck(_isDisposed, nameof(MQ2SubmoduleEventRegistry));
				_onSetGameState += value;
			}
			remove
			{
				CleanupHelper.DisposedCheck(_isDisposed, nameof(MQ2SubmoduleEventRegistry));
				_onSetGameState -= value;
			}
		}



		private event EventHandler? _onZoned;

		/// <summary>
		/// Similar/same as EndZone ?
		/// </summary>
		public event EventHandler? OnZoned
		{
			add
			{
				CleanupHelper.DisposedCheck(_isDisposed, nameof(MQ2SubmoduleEventRegistry));
				_onZoned += value;
			}
			remove
			{
				CleanupHelper.DisposedCheck(_isDisposed, nameof(MQ2SubmoduleEventRegistry));
				_onZoned -= value;
			}
		}



		// Convenience methods called from the loader entry point to signal the events to submodules
		internal void NotifyAddGroundItem(GroundType groundItem)
		{
			if (!_isDisposed)
			{
				_onAddGroundItem?.Invoke(this, groundItem);
			}
		}

		internal void NotifyAddSpawn(SpawnType newSpawn)
		{
			if (!_isDisposed)
			{
				_onAddSpawn?.Invoke(this, newSpawn);
			}
		}

		internal void NotifyBeginZone(EventArgs eventArgs)
		{
			if (!_isDisposed)
			{
				_onBeginZone?.Invoke(this, eventArgs);
			}
		}

		internal void NotifyChatAny(ChatLineEventArgs chatLineEventArgs)
		{
			if (!_isDisposed)
			{
				_onChatAny?.Invoke(this, chatLineEventArgs);
			}
		}

		internal void NotifyChatEQ(ChatLineEventArgs chatLineEventArgs)
		{
			if (!_isDisposed)
			{
				_onChatEQ?.Invoke(this, chatLineEventArgs);
			}
		}

		internal void NotifyChatMQ2(ChatLineEventArgs chatLineEventArgs)
		{
			if (!_isDisposed)
			{
				_onChatMQ2?.Invoke(this, chatLineEventArgs);
			}
		}

		internal void NotifyCleanUI(EventArgs eventArgs)
		{
			if (!_isDisposed)
			{
				_onCleanUI?.Invoke(this, eventArgs);
			}
		}

		internal void NotifyDrawHUD(EventArgs eventArgs)
		{
			if (!_isDisposed)
			{
				_onDrawHUD?.Invoke(this, eventArgs);
			}
		}

		internal void NotifyEndZone(EventArgs eventArgs)
		{
			if (!_isDisposed)
			{
				_onEndZone?.Invoke(this, eventArgs);
			}
		}

		internal void NotifyReloadUI(EventArgs eventArgs)
		{
			if (!_isDisposed)
			{
				_onReloadUI?.Invoke(this, eventArgs);
			}
		}

		internal void NotifyRemoveGroundItem(GroundType removedGroundItem)
		{
			if (!_isDisposed)
			{
				_onRemoveGroundItem?.Invoke(this, removedGroundItem);
			}
		}

		internal void NotifyRemoveSpawn(SpawnType removedSpawn)
		{
			if (!_isDisposed)
			{
				_onRemoveSpawn?.Invoke(this, removedSpawn);
			}
		}

		internal void NotifySetGameState(GameState gameState)
		{
			if (!_isDisposed)
			{
				_onSetGameState?.Invoke(this, gameState);
			}
		}

		internal void NotifyZoned(EventArgs eventArgs)
		{
			if (!_isDisposed)
			{
				_onZoned?.Invoke(this, eventArgs);
			}
		}
	}
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore CA1030 // Use events where appropriate
}
