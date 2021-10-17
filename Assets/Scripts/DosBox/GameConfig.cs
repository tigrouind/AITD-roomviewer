public struct GameConfig
{
	public int ActorsAddress;
	public int ActorStructSize;
	public int TrackModeOffset;

	public GameConfig(int actorsAddress, int actorStructSize, int trackModeOffset)
	{
		ActorsAddress = actorsAddress;
		ActorStructSize = actorStructSize;
		TrackModeOffset = trackModeOffset;
	}
}