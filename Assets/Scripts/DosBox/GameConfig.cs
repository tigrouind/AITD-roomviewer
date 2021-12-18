public class GameConfig
{
	public readonly int ActorsAddress;
	public readonly int ActorStructSize;
	public readonly int TrackModeOffset;
	public readonly int ActorsPointer;

	public GameConfig(int actorsAddress, int actorStructSize, int trackModeOffset, int actorsPointer = 0)
	{
		ActorsAddress = actorsAddress;
		ActorStructSize = actorStructSize;
		TrackModeOffset = trackModeOffset;
		ActorsPointer = actorsPointer;
	}
}