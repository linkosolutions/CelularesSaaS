namespace CelularesSaaS.Domain.Common;

public interface IAuditableEntity
{
    Guid? CreadoPorUsuarioId { get; set; }
    Guid? ModificadoPorUsuarioId { get; set; }
}
