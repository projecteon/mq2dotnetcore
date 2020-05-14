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

		private event EventHandler<string>? _onChatEQ;

		/// <summary>
		/// Fired on a line of chat from EQ
		/// </summary>
		public event EventHandler<string>? OnChatEQ
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


		private event EventHandler<string>? _onChatMQ2;

		/// <summary>
		/// Fired on a line of chat from MQ2
		/// </summary>
		public event EventHandler<string>? OnChatMQ2
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



		private event EventHandler<string>? _onChatAny;

		/// <summary>
		/// Fired from a line of chat from either EQ or MQ2
		/// </summary>
		public event EventHandler<string>? OnChatAny
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

		internal void NotifyBeginZone(object sender, EventArgs e)
		{
			if (!_isDisposed)
			{
				_onBeginZone?.Invoke(this, e);
			}
		}

		private void NotifyEndZone(object sender, EventArgs e)
		{
			if (!_isDisposed)
			{
				_onEndZone?.Invoke(this, e);
			}
		}

		private void NotifyOnAddGroundItem(object sender, GroundType e)
		{
			if (!_isDisposed)
			{
				_onAddGroundItem?.Invoke(this, e);
			}
		}

		private void NotifyOnAddSpawn(object sender, SpawnType e)
		{
			if (!_isDisposed)
			{
				_onAddSpawn?.Invoke(this, e);
			}
		}

		private void NotifyOnChat(object sender, string e)
		{
			if (!_isDisposed)
			{
				_onChatAny?.Invoke(this, e);
			}
		}

		private void NotifyOnChatEQ(object sender, string e)
		{
			if (!_isDisposed)
			{
				_onChatEQ?.Invoke(this, e);
			}
		}

		private void NotifyOnChatMQ2(object sender, string e)
		{
			if (!_isDisposed)
			{
				_onChatMQ2?.Invoke(this, e);
			}
		}

		private void NotifyOnCleanUI(object sender, EventArgs e)
		{
			if (!_isDisposed)
			{
				_onCleanUI?.Invoke(this, e);
			}
		}

		private void NotifyOnDrawHUD(object sender, EventArgs e)
		{
			if (!_isDisposed)
			{
				_onDrawHUD?.Invoke(this, e);
			}
		}

		private void NotifyOnReloadUI(object sender, EventArgs e)
		{
			if (!_isDisposed)
			{
				_onReloadUI?.Invoke(this, e);
			}
		}

		private void NotifyOnRemoveGroundItem(object sender, GroundType e)
		{
			if (!_isDisposed)
			{
				_onRemoveGroundItem?.Invoke(this, e);
			}
		}

		private void NotifyOnRemoveSpawn(object sender, SpawnType e)
		{
			if (!_isDisposed)
			{
				_onRemoveSpawn?.Invoke(this, e);
			}
		}

		private void NotifyOnZoned(object sender, EventArgs e)
		{
			if (!_isDisposed)
			{
				_onZoned?.Invoke(this, e);
			}
		}

		private void NotifySetGameState(object sender, GameState e)
		{
			if (!_isDisposed)
			{
				_onSetGameState?.Invoke(this, e);
			}
		}
	}
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore CA1030 // Use events where appropriate
}
