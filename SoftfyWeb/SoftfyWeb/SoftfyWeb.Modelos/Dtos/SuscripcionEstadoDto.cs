namespace SoftfyWeb.Modelos.Dtos
{
    public class SuscripcionEstadoDto
    {
        public string Tipo { get; set; } // "Free" o "Premium"
        public string Plan { get; set; }
        public decimal Precio { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public bool EsTitular { get; set; } // Si el usuario es el titular de la suscripción
    }
}
