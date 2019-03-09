using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Xml.Dom;
using static ArticlesApp.Classes.Models;

namespace ArticlesApp.Classes
{
	public class ArticlesRss
	{

		public class RssElement
		{
			public bool Completed { get; set; }
											   //le pattern
			const string ID_PATTERN = "https://www.supinfo.com/articles/single/";
			public Category Category { get; set; }
			public Uri EndPoint { get; set; }

			private List<Article> _articles = new List<Article>();

			public List<Article> Articles {
				get { return _articles; }
				set { this._articles = value; }
			}

			public async Task<bool> Fetch()
			{
				// création de l'objet xmldocument
				XmlDocument xmlDoc;

				try
				{
					// recuperer la ressource en ligne en se basant sur le endpoint
					xmlDoc = await XmlDocument.LoadFromUriAsync(this.EndPoint);
				}
				catch (Exception ex)
				{
					// en cas d'erreur on retourne false
					var message = ex.Message;
					return false;
				}
				// récuperation de chaque Node
				IXmlNode categoryTitleNode = xmlDoc.SelectSingleNode("//rss/channel/title");
				IXmlNode categoryDescriptionNode = xmlDoc.SelectSingleNode("//rss/channel/description");
				IXmlNode categoryLinkNode = xmlDoc.SelectSingleNode("//rss/channel/link");
				IXmlNode categoryLanguageNode = xmlDoc.SelectSingleNode("//rss/channel/language");
				IXmlNode categoryCopyrightNode = xmlDoc.SelectSingleNode("//rss/channel/copyright");
				// assigner les bonnes valeurs de category
				this.Category.Title = categoryTitleNode.InnerText.ToString();
				this.Category.Description = categoryDescriptionNode.InnerText.ToString();
				this.Category.Link = categoryLinkNode.InnerText.ToString();
				this.Category.Language = categoryLanguageNode.InnerText.ToString();
				this.Category.Copyright = categoryCopyrightNode.InnerText.ToString();

				//récuper la liste des item (article)
				XmlNodeList itemNodes = xmlDoc.SelectNodes("//rss/channel/item");

				// parcourrir la liste des itemnodes
				foreach(IXmlNode itemNode in itemNodes)
				{
					// recuperer les itemdes
					IXmlNode titleNode = itemNode.SelectSingleNode("title");
					IXmlNode linkNode = itemNode.SelectSingleNode("link");
					IXmlNode descriptionNode = itemNode.SelectSingleNode("description");
					IXmlNode pubDateNode = itemNode.SelectSingleNode("pubDate");
					//si l'un des itemNodes est null  alors on 
					//passe à l'element suivant de la liste
					if (titleNode ==null  || linkNode == null 
						|| descriptionNode ==null || pubDateNode==null)
					{
						continue;
					}
					int articleId = int.Parse(linkNode.InnerText.ToString().
						Replace(ID_PATTERN, "").Split('-').First());
					// quelque chose à  faire
					this._articles.Add(
						new Article()
						{
							Id = articleId,
							Title = titleNode.InnerText.ToString(),
							Description = descriptionNode.InnerText.ToString(),
							Link = linkNode.InnerText.ToString(),
							PubDate = DateTime.Parse( pubDateNode.InnerText.ToString()),
							Category = this.Category
						}
					);
				}
				return true;

				//https://www.supinfo.com/articles/single/{identifiant}-{titre de l'article}

			}
		}


		public class RssReader
		{
			const string ARTICLES_URI = "https://www.supinfo.com/api/supinfo?action=rss&tags=";
			private static RssReader _inst;
			private List<RssElement> _elements = new List<RssElement>();
			public List<RssElement> Elements => this._elements;

			public event EventHandler ElementsChanged;
			public event EventHandler LoadCompleted;

			public static RssReader GetInstance()
			{
				if (_inst == null) _inst = new RssReader();
				return _inst;
			}


			private RssReader()
			{
				Start();
			}

			private void Start()
			{
				string uri = ARTICLES_URI;
				for (int i = 1; i < 20; i++)
				{
					Load(new Uri(uri + i), new Category() { Id = i });
				}
			}

			private async void Load(Uri uri, Category c)
			{
				RssElement e = new RssElement()
				{
					Category = c,
					EndPoint = uri
				};
				Elements.Add(e);
				await e.Fetch();

				e.Completed = true;
				OnElementElementCompleted(e);
			}


			private  void OnElementElementCompleted(RssElement e)
			{
				ElementsChanged?.Invoke(e, EventArgs.Empty);
				if (Elements.Where(r=> r.Completed == false ).FirstOrDefault() ==null)
				{
					LoadCompleted?.Invoke(this, EventArgs.Empty);
				}
			}


		}

	}
}
