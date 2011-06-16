namespace Lokad.Cqrs.Extensions.Web.Security
{
    interface IPasswordCodec
    {
        string Encode(string password);
        string Decode(string password);
    }
}