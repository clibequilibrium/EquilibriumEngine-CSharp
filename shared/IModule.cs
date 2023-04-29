using Arch.Core;

public interface IModule
{
    List<ISystem> Initialize(World world);
}