namespace MQ2DotNetCore.MQ2Api
{
	/// <summary>
	/// State of the game, e.g. char select, in game
	/// </summary>
	public enum GameState : uint
	{
		CharSelect = 1,
		CharCreate = 2,
		Something = 4,
		InGame = 5,
		PreCharSelect = uint.MaxValue,
		PostFrontLoad = 500,
		LoggingIn = 253,
		Unloading = 255,
		Unknown = 65535
	}
}
