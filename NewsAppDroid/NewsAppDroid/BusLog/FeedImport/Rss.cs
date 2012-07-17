/*
 * This file is part of ADFC-NewsApp
 * Copyright (C) 2012 David Hoffmann
 *
 * ADFC-NewsApp is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation, version 2.
 *
 * ADFC-NewsApp is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with ADFC-NewsApp; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 * 
 */


using System;
using de.dhoffmann.mono.adfcnewsapp.buslog.webservice;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using System.Linq;
#if MONODROID
using Android.Text;
#endif

namespace de.dhoffmann.mono.adfcnewsapp.buslog.feedimport
{
	public class Rss
	{
		
		public class RssFeed
		{
			public RssHeader Header { get; set; }
			public List<RssItem> Items { get; set; }
		}
		
		public class RssHeader
		{
			public int FeedID { get; set; }
			public string Title { get; set; }
			public string Link { get; set; }
			public string Description { get; set; }
			public DateTime? LastBuildDate { get; set; }
		}
		
		public class RssItem
		{
			public int ItemID { get; set; }
			public int FeedID { get; set; }
			public string Title { get; set; }
			public string Link { get; set; }
			public string Description { get; set; }
			public string Category { get; set; }
			public DateTime? PubDate { get; set; }
			public bool IsRead { get; set; }
		}
		
		
		public Rss ()
		{
		}
		
		public bool ImportRss(WSFeedConfig.FeedConfig feed, string webSource)
		{
			bool ret = false;
			
			System.Diagnostics.Debug.WriteLine(string.Format("ImportRss: \"{0}\")", feed.Name));
			
			RssFeed rssFeed = ReadRssFeed(feed.FeedID, webSource);
			
			if (rssFeed != null && rssFeed.Header != null)
				ret = new database.Rss().SetRssFeed(rssFeed);
			else 
				ret = true;
			
			return ret;
		}
		
		private RssFeed ReadRssFeed(int feedID, string webSource)
		{
			database.Rss dbRss = new de.dhoffmann.mono.adfcnewsapp.buslog.database.Rss();
			RssFeed ret = new RssFeed();
			
			try
			{
				XDocument doc = XDocument.Parse(webSource);
				
				var channel = doc.Descendants("channel");
				
				if (channel != null)
				{
					var qTitle = channel.Descendants("title").First();
					string title = null;

#if MONODROID
					if (qTitle != null)
						title = Html.FromHtml(qTitle.Value).ToString().Replace("￼", "").Trim();
#else
					if (qTitle != null)
						title = qTitle.Value.ToString().Trim();
#endif
					
					var qLink = channel.Descendants("link").First();
					string link = null;
					
					if (qLink != null)
						link = qLink.Value;
					
					var qDescription = channel.Descendants("description").First();
					string description = null;

#if MONODROID
					if (qDescription != null)
						description = Html.FromHtml(qDescription.Value).ToString().Replace("￼", "").Trim();
#else
					if (qDescription != null)
						description = qDescription.Value.ToString().Trim();
#endif
					DateTime lastBuildDate = DateTime.MinValue;
					try
					{
						var qLastBuildDate = channel.Descendants("lastBuildDate").First();

						// TODO prüfen
						if (qLastBuildDate != null)
							lastBuildDate = DateTime.Parse(qLastBuildDate.Value);
					}
					catch(Exception)
					{ ; }

					ret.Header = new RssHeader()
					{
						FeedID = feedID,
						Title = title,
						Link = link,
						Description = description,
						LastBuildDate = lastBuildDate
					};
					
					// Das Datum der letzten Daten importieren
					DateTime? dbLastBuildDate = dbRss.GetLastBuildDate(feedID);
					dbLastBuildDate = null;

					// Müssen die Items wirklich geparsed werden?
					if(!dbLastBuildDate.HasValue || (ret != null && ret.Header != null && ret.Header.LastBuildDate.HasValue && dbLastBuildDate.Value < ret.Header.LastBuildDate.Value))
					{
						var items = doc.Descendants("item");
						
						if (items != null)
						{
							List<RssItem> rssItems = new List<RssItem>();
							
							foreach (var item in items)
							{
								RssItem rssItem = new RssItem();
								
								rssItem.FeedID = feedID;
								
								var qiTitle = item.Descendants("title").First();
#if MONODROID
								if (qiTitle != null)
									rssItem.Title = Html.FromHtml(qiTitle.Value).ToString().Replace("￼", "").Trim();
#else
								if (qiTitle != null)
									rssItem.Title = qiTitle.Value.ToString().Trim();
#endif
								
								var qiLink = item.Descendants("link").First();
								if (qiLink != null)
									rssItem.Link = qiLink.Value;
								
								var qiDescription = item.Descendants("description").First();
#if MONODROID
								if (qiDescription != null)
									rssItem.Description = Html.FromHtml(qiDescription.Value).ToString().Replace("￼", "").Trim();
#else
								if (qiDescription != null)
									rssItem.Description = qiDescription.Value.ToString().Trim();
#endif

								// Nicht alle haben diesen Node
								try
								{
									var qiCategory = item.Descendants("category").First();
#if MONODROID
									if (qiCategory != null)
										rssItem.Category = Html.FromHtml(qiCategory.Value).ToString().Replace("￼", "").Trim();
#else
									if (qiCategory != null)
										rssItem.Category = qiCategory.Value.ToString().Trim();
#endif
								}
								catch(Exception)
								{ ; }

								try
								{
									// TODO prüfen!!
									var qiPubDate = item.Descendants("pubDate").First();
									if (qiPubDate != null)
										rssItem.PubDate = DateTime.Parse(qiPubDate.Value);
								}
								catch(Exception)
								{ ; }
								
								rssItems.Add(rssItem);
							}
							
							ret.Items = rssItems;
						}
					}
				}
			}
			catch(Exception ex)
			{
				System.Diagnostics.Debug.WriteLine("ex: " + ex.ToString());
			}
			
			return ret;
		}
	
	}
}

