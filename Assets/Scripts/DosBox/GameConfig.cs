[System.Serializable]
public class GameConfig
{
	public int ActorsAddress;
	public int ActorStructSize;
	public int TrackModeOffset;
	public int ActorsPointer;

	public GameConfig(int actorsAddress, int actorStructSize, int trackModeOffset, int actorsPointer = 0)
	{
		ActorsAddress = actorsAddress;
		ActorStructSize = actorStructSize;
		TrackModeOffset = trackModeOffset;
		ActorsPointer = actorsPointer;
	}
}