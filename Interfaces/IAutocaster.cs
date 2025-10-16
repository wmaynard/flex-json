using System.ComponentModel.DataAnnotations;

namespace Maynard.Interfaces;

public interface IAutocaster
{
    public T Require<T>(string key);
    public object Require(string key);
    public T Optional<T>(string key);
    public object Optional(string key);
}