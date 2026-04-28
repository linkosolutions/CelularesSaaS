namespace CelularesSaaS.Domain.Enums;

public enum CondicionEquipo { Nuevo = 1, Usado = 2, Reacondicionado = 3 }

public enum EstadoEquipo
{
    EnStock = 1, Reservado = 2, Vendido = 3,
    EnReparacion = 4, EnGarantia = 5, Devuelto = 6, DadoDeBaja = 7
}

public enum UbicacionEquipo { Vitrina = 1, Deposito = 2, EnTransito = 3 }

public enum Moneda { ARS = 1, USD = 2, PEN = 3, BRL = 4, CLP = 5, UYU = 6, EUR = 7 }

public enum FormaPago
{
    Efectivo = 1, Transferencia = 2, Debito = 3, Credito = 4,
    MercadoPago = 5, Dolares = 6, PartePago = 7, Cripto = 8,
    Soles = 9, Reales = 10, PesoChileno = 11, PesoUruguayo = 12, Euros = 13,
    Otro = 99
}

public enum EstadoReparacion
{
    Ingresado = 1, EnDiagnostico = 2, PresupuestoEnviado = 3,
    Aprobado = 4, EnReparacion = 5, Reparado = 6,
    Entregado = 7, NoReparable = 8, Rechazado = 9
}

public enum TipoItemVenta { Equipo = 1, Accesorio = 2, Servicio = 3 }

public enum RolUsuario
{
    SuperAdmin = 1, AdminTenant = 2, Vendedor = 3, Tecnico = 4, SoloLectura = 5
}
public enum EstadoVenta
{
    Completada = 1,
    Reservada = 2,
    Pendiente = 3,
    Anulada = 4,
}
public enum EstadoCita
{
    Pendiente = 1,
    Confirmada = 2,
    Cancelada = 3,
    Completada = 4,
}
