using CelularesSaaS.Domain.Common;
namespace CelularesSaaS.Domain.Entities;
public class Anuncio : BaseEntity
{
    public string Titulo { get; set; } = null!;
    public string Contenido { get; set; } = null!;
    public bool Activo { get; set; } = false;
    public string Tipo { get; set; } = "Info"; // Info, Advertencia, Exito, Alerta
}