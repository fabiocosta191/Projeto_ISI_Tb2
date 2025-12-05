using System.ComponentModel.DataAnnotations;

namespace SafeHome.API.DTOs
{
    /// <summary>
    /// Modelo de dados para criar um novo sensor.
    /// </summary>
    public class CreateSensorDto
    {
        /// <summary>
        /// O nome descritivo do sensor (ex: "Detetor Fumo Cozinha").
        /// </summary>
        [Required(ErrorMessage = "O nome é obrigatório")]
        [StringLength(100, ErrorMessage = "O nome não pode exceder 100 caracteres")]
        public string Name { get; set; }

        /// <summary>
        /// O tipo de sensor (ex: Smoke, Temperature, Flood).
        /// </summary>
        [Required(ErrorMessage = "O tipo é obrigatório")]
        public string Type { get; set; }

        /// <summary>
        /// O ID do edifício onde o sensor será instalado. (Opcional)
        /// </summary>
        public int? BuildingId { get; set; }
    }

    /// <summary>
    /// Modelo de dados para atualizar um sensor existente.
    /// </summary>
    public class UpdateSensorDto
    {
        /// <summary>
        /// O novo nome do sensor.
        /// </summary>
        [Required(ErrorMessage = "O nome é obrigatório")]
        public string Name { get; set; }

        /// <summary>
        /// O novo tipo do sensor.
        /// </summary>
        [Required(ErrorMessage = "O tipo é obrigatório")]
        public string Type { get; set; }

        /// <summary>
        /// Indica se o sensor está ativo ou inativo.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// O ID do edifício associado. (Opcional)
        /// </summary>
        public int? BuildingId { get; set; }
    }
}