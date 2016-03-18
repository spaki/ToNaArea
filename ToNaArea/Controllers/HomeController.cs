using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity.Spatial;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using ToNaArea.Models;

namespace ToNaArea.Controllers
{
    public class HomeController : Controller
    {
        ApplicationDbContext bancoDeDados;

        public HomeController()
        {
            bancoDeDados = new ApplicationDbContext();
        }

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {
            return View();
        }

        public ActionResult LoginPartial()
        {
            return PartialView();
        }

        public ActionResult ObterUltimosUploads(int? ultimoId, string latitude, string longitude)
        {
            if (string.IsNullOrWhiteSpace(latitude) || string.IsNullOrWhiteSpace(longitude))
                return Json(new { TemResultado = false }, JsonRequestBehavior.AllowGet);

            int distanciEmKm = 2 * 1000 * 1000; // 2 mil km
            DbGeography localizacao = DbGeography.FromText(string.Format("POINT({0} {1})", longitude, latitude));

            IQueryable<Arquivo> consulta = from e in bancoDeDados.Arquivos
                                           let distance = e.Localizacao.Distance(localizacao)
                                           where distance <= distanciEmKm
                                           select e;

            if (ultimoId.HasValue)
                consulta = consulta.Where(e => e.Id > ultimoId);

            consulta = consulta.OrderByDescending(e => e.Timestamp).Take(100);

            var resultadoDaConsulta = consulta
                .ToList()
                .Select(e => new
                {
                    UrlFotoDoFacebook = e.ObterUrlDaFotoDoFacebook(16),
                    Timestamp = e.Timestamp.ToString(),
                    e.FacebookNome,
                    e.Status,
                    e.UrlImagem,
                    e.Localizacao.Latitude,
                    e.Localizacao.Longitude,
                    e.Id
                });

            var resultado = new
            {
                Items = resultadoDaConsulta,
                UltimoId = resultadoDaConsulta.Any() ? resultadoDaConsulta.Max(e => e.Id) : (int?)null,
                TemResultado = resultadoDaConsulta.Any()
            };

            return Json(resultado, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Detalhes(int id)
        {
            var entidade = bancoDeDados.Arquivos.FirstOrDefault(e => e.Id == id);
            return View(entidade);
        }

        [Authorize]
        public ActionResult Upload(string status, string latitude, string longitude, HttpPostedFileBase foto)
        {
            string caminhoDaImagem = SalvarNaNuvem(foto);
            SarvarInformacoesNoBancoDeDados(status, latitude, longitude, caminhoDaImagem);

            TempData.Add("Sucesso", "Foto enviada com sucesso!");

            return RedirectToAction("Index");
        }

        [Authorize]
        public ActionResult FotoUsuarioLogado()
        {
            IEnumerable<Claim> claims = ((ClaimsIdentity)User.Identity).Claims;
            string facebookId = claims.FirstOrDefault(c => c.Type == "FacebookId").Value;
            string url = Arquivo.ObterUrlDaFotoDoFacebook(facebookId, 16);

            return Redirect(url);
        }

        private string SalvarNaNuvem(HttpPostedFileBase foto)
        {
            CloudStorageAccount conta = RecuperarContaDaNuvem();
            CloudBlobClient cliente = conta.CreateCloudBlobClient();
            CloudBlobContainer container = ObterContainer(cliente);

            string nome = GerarNome(foto);

            CloudBlockBlob blockBlob = container.GetBlockBlobReference(nome);

            blockBlob.Properties.ContentType = foto.ContentType;
            blockBlob.UploadFromStream(foto.InputStream);

            return blockBlob.Uri.AbsoluteUri;
        }

        private string GerarNome(HttpPostedFileBase foto)
        {
            string identificador = Guid.NewGuid().ToString();
            string extensao = Path.GetExtension(foto.FileName);
            string nome = string.Format("tonaarea/{0}{1}", identificador, extensao);

            return nome;
        }

        private CloudStorageAccount RecuperarContaDaNuvem()
        {
            string connectionString = ConfigurationManager.AppSettings["AzureStorageConnectionString"];
            CloudStorageAccount result = CloudStorageAccount.Parse(connectionString);
            return result;
        }

        private CloudBlobContainer ObterContainer(CloudBlobClient cliente)
        {
            CloudBlobContainer container = cliente.GetContainerReference("images");
            container.CreateIfNotExists();

            BlobContainerPermissions permissao = new BlobContainerPermissions();
            permissao.PublicAccess = BlobContainerPublicAccessType.Blob;

            container.SetPermissions(permissao);

            return container;
        }

        private void SarvarInformacoesNoBancoDeDados(string status, string latitude, string longitude, string caminhoDaImagem)
        {
            ClaimsIdentity identidade = (ClaimsIdentity)User.Identity;

            Arquivo arquivo = new Arquivo();
            arquivo.Status = status;
            arquivo.UrlImagem = caminhoDaImagem;
            arquivo.Timestamp = DateTime.Now;

            arquivo.FacebookId = identidade.Claims.FirstOrDefault(c => c.Type == "FacebookId").Value;
            arquivo.FacebookNome = identidade.Claims.FirstOrDefault(c => c.Type == "FacebookNome").Value;

            arquivo.Localizacao = DbGeography.FromText(string.Format("POINT({0} {1})", longitude, latitude));

            bancoDeDados.Arquivos.Add(arquivo);
            bancoDeDados.SaveChanges();
        }
    }
}