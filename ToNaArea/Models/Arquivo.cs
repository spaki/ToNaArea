using System;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity.Spatial;

namespace ToNaArea.Models
{
    public class Arquivo
    {
        [Key]
        public int Id { get; set; }

        public string Status { get; set; }
        public string UrlImagem { get; set; }
        public string FacebookId { get; set; }
        public string FacebookNome { get; set; }

        public DateTime Timestamp { get; set; }

        public DbGeography Localizacao { get; set; }

        public string ObterUrlDaFotoDoFacebook(int tamanho)
        {
            return ObterUrlDaFotoDoFacebook(FacebookId, tamanho);
        }

        public static string ObterUrlDaFotoDoFacebook(string facebookId, int tamanho)
        {
            string url = $"https://graph.facebook.com/{facebookId}/picture?type=square&width={tamanho}";
            return url;
        }
    }
}