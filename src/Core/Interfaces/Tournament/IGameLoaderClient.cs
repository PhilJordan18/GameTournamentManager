namespace Core.Interfaces.Tournament;

public interface IGameLoaderClient
{
    Task<int> LoadGameAsync(int pageSize = 20, int maxPage = 5);
}