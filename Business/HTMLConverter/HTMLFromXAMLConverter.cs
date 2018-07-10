//---------------------------------------------------------------------------
// 
// File: HtmlFromXamlConverter.cs
//
// Copyright (C) Microsoft Corporation.  All rights reserved.
//
// Description: Prototype for Xaml - Html conversion 
//
//---------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml;

namespace Inflectra.SpiraTest.IDEIntegration.VisualStudio2012.Business.HTMLandXAML
{

	/// <summary> HtmlToXamlConverter is a static class that takes an HTML string and converts it into XAML </summary>
	public static class HtmlFromXamlConverter
	{
		// ---------------------------------------------------------------------
		//
		// Internal Methods
		//
		// ---------------------------------------------------------------------

		#region Internal Methods

		/// <summary>Main entry point for XAML-to-HTML converter. Converts a XAML string into html string.</summary>
		/// <param name="xamlString">XAML string to convert.</param>
		/// <returns>Html string produced from a source XAML.</returns>
		public static string ConvertXamlToHtml(string xamlString)
		{
			XmlTextReader xamlReader;
			StringBuilder htmlStringBuilder;
			XmlTextWriter htmlWriter;

			xamlReader = new XmlTextReader(new StringReader(xamlString));

			htmlStringBuilder = new StringBuilder(100);
			htmlWriter = new XmlTextWriter(new StringWriter(htmlStringBuilder));

			if (!WriteFlowDocument(xamlReader, htmlWriter))
			{
				return "";
			}

			string htmlString = htmlStringBuilder.ToString();

			return htmlString;
		}

		#endregion Internal Methods

		// ---------------------------------------------------------------------
		//
		// Private Methods
		//
		// ---------------------------------------------------------------------

		#region Private Methods
		/// <summary>
		/// Processes a root level element of XAML (normally it's FlowDocument element).
		/// </summary>
		/// <param name="xamlReader">
		/// XmlTextReader for a source xaml.
		/// </param>
		/// <param name="htmlWriter">
		/// XmlTextWriter producing resulting html
		/// </param>
		private static bool WriteFlowDocument(XmlTextReader xamlReader, XmlTextWriter htmlWriter)
		{
			if (!ReadNextToken(xamlReader))
			{
				// Xaml content is empty - nothing to convert
				return false;
			}

			if (xamlReader.NodeType != XmlNodeType.Element || xamlReader.Name != "FlowDocument")
			{
				// Root FlowDocument elemet is missing
				return false;
			}

			// Create a buffer StringBuilder for collecting css properties for inline STYLE attributes
			// on every element level (it will be re-initialized on every level).
			StringBuilder inlineStyle = new StringBuilder();

			htmlWriter.WriteStartElement("html");
			htmlWriter.WriteStartElement("body");

			WriteFormattingProperties(xamlReader, htmlWriter, inlineStyle);

			WriteElementContent(xamlReader, htmlWriter, inlineStyle);

			htmlWriter.WriteEndElement();
			htmlWriter.WriteEndElement();

			return true;
		}

		/// <summary>
		/// Reads attributes of the current xaml element and converts
		/// them into appropriate html attributes or css styles.
		/// </summary>
		/// <param name="xamlReader">
		/// XmlTextReader which is expected to be at XmlNodeType.Element
		/// (opening element tag) position.
		/// The reader will remain at the same level after function complete.
		/// </param>
		/// <param name="htmlWriter">
		/// XmlTextWriter for output html, which is expected to be in
		/// after WriteStartElement state.
		/// </param>
		/// <param name="inlineStyle">
		/// String builder for collecting css properties for inline STYLE attribute.
		/// </param>
		private static void WriteFormattingProperties(XmlTextReader xamlReader, XmlTextWriter htmlWriter, StringBuilder inlineStyle)
		{
			Debug.Assert(xamlReader.NodeType == XmlNodeType.Element);

			// Clear string builder for the inline style
			inlineStyle.Remove(0, inlineStyle.Length);

			bool isHyperlink = (xamlReader.Name == "Hyperlink");

			if (!xamlReader.HasAttributes)
			{
				return;
			}

			bool borderSet = false;

			while (xamlReader.MoveToNextAttribute())
			{
				string css = null;

				switch (xamlReader.Name)
				{
					// Character fomatting properties
					// ------------------------------
					case "Background":
						css = "background-color:" + ParseXamlColor(xamlReader.Value) + ";";
						break;
					case "FontFamily":
						css = "font-family:" + xamlReader.Value + ";";
						break;
					case "FontStyle":
						css = "font-style:" + xamlReader.Value.ToLower() + ";";
						break;
					case "FontWeight":
						css = "font-weight:" + xamlReader.Value.ToLower() + ";";
						break;
					case "FontStretch":
						break;
					case "FontSize":
						css = "font-size:" + xamlReader.Value + ";";
						break;
					case "Foreground":
						//We don't want goreground on a link. Filter it out.
						if (!isHyperlink)
							css = "color:" + ParseXamlColor(xamlReader.Value) + ";";
						break;
					case "TextDecorations":
						if (!isHyperlink)
							css = "text-decoration:underline;";
						break;
					case "TextEffects":
						break;
					case "Emphasis":
						break;
					case "StandardLigatures":
						break;
					case "Variants":
						break;
					case "Capitals":
						break;
					case "Fraction":
						break;

					// Paragraph formatting properties
					// -------------------------------
					case "Padding":
						css = "padding:" + ParseXamlThickness(xamlReader.Value) + ";";
						break;
					case "Margin":
						css = "margin:" + ParseXamlThickness(xamlReader.Value) + ";";
						break;
					case "BorderThickness":
						css = "border-width:" + ParseXamlThickness(xamlReader.Value) + ";";
						borderSet = true;
						break;
					case "BorderBrush":
						css = "border-color:" + ParseXamlColor(xamlReader.Value) + ";";
						borderSet = true;
						break;
					case "LineHeight":
						break;
					case "TextIndent":
						css = "text-indent:" + xamlReader.Value + ";";
						break;
					case "TextAlignment":
						css = "text-align:" + xamlReader.Value + ";";
						break;
					case "IsKeptTogether":
						break;
					case "IsKeptWithNext":
						break;
					case "ColumnBreakBefore":
						break;
					case "PageBreakBefore":
						break;
					case "FlowDirection":
						break;

					// Table attributes
					// ----------------
					case "Width":
						css = "width:" + xamlReader.Value + ";";
						break;
					case "ColumnSpan":
						htmlWriter.WriteAttributeString("colspan", xamlReader.Value);
						break;
					case "RowSpan":
						htmlWriter.WriteAttributeString("rowspan", xamlReader.Value);
						break;

					// Link attributes
					// ---------------
					case "NavigateUri":
						htmlWriter.WriteAttributeString("href", xamlReader.Value);
						htmlWriter.WriteAttributeString("target", "_blank");
						break;
				}

				if (css != null)
				{
					inlineStyle.Append(css);
				}
			}

			if (borderSet)
			{
				inlineStyle.Append("border-style:solid;mso-element:para-border-div;");
			}

			// Return the xamlReader back to element level
			xamlReader.MoveToElement();
			Debug.Assert(xamlReader.NodeType == XmlNodeType.Element);
		}

		private static string ParseXamlColor(string color)
		{
			if (color.StartsWith("#"))
			{
				// Remove transparancy value
				color = "#" + color.Substring(3);
			}
			return color;
		}

		private static string ParseXamlThickness(string thickness)
		{
			string[] values = thickness.Split(',');

			for (int i = 0; i < values.Length; i++)
			{
				double value;
				if (double.TryParse(values[i], out value))
				{
					values[i] = Math.Ceiling(value).ToString();
				}
				else
				{
					values[i] = "1";
				}
			}

			string cssThickness;
			switch (values.Length)
			{
				case 1:
					cssThickness = thickness;
					break;
				case 2:
					cssThickness = values[1] + " " + values[0];
					break;
				case 4:
					cssThickness = values[1] + " " + values[2] + " " + values[3] + " " + values[0];
					break;
				default:
					cssThickness = values[0];
					break;
			}

			return cssThickness;
		}

		/// <summary>
		/// Reads a content of current xaml element, converts it
		/// </summary>
		/// <param name="xamlReader">
		/// XmlTextReader which is expected to be at XmlNodeType.Element
		/// (opening element tag) position.
		/// </param>
		/// <param name="htmlWriter">
		/// May be null, in which case we are skipping the xaml element;
		/// witout producing any output to html.
		/// </param>
		/// <param name="inlineStyle">
		/// StringBuilder used for collecting css properties for inline STYLE attribute.
		/// </param>
		private static void WriteElementContent(XmlTextReader xamlReader, XmlTextWriter htmlWriter, StringBuilder inlineStyle)
		{
			Debug.Assert(xamlReader.NodeType == XmlNodeType.Element);

			bool elementContentStarted = false;

			if (xamlReader.IsEmptyElement)
			{
				if (htmlWriter != null && !elementContentStarted && inlineStyle.Length > 0)
				{
					// Output STYLE attribute and clear inlineStyle buffer.
					htmlWriter.WriteAttributeString("style", inlineStyle.ToString());
					inlineStyle.Remove(0, inlineStyle.Length);
				}
				elementContentStarted = true;
			}
			else
			{
				while (ReadNextToken(xamlReader) && xamlReader.NodeType != XmlNodeType.EndElement)
				{
					switch (xamlReader.NodeType)
					{
						case XmlNodeType.Element:
							if (xamlReader.Name.Contains("."))
							{
								AddComplexProperty(xamlReader, inlineStyle);
							}
							else
							{
								if (htmlWriter != null && !elementContentStarted && inlineStyle.Length > 0)
								{
									// Output STYLE attribute and clear inlineStyle buffer.
									htmlWriter.WriteAttributeString("style", inlineStyle.ToString());
									inlineStyle.Remove(0, inlineStyle.Length);
								}
								elementContentStarted = true;
								WriteElement(xamlReader, htmlWriter, inlineStyle);
							}
							Debug.Assert(xamlReader.NodeType == XmlNodeType.EndElement || xamlReader.NodeType == XmlNodeType.Element && xamlReader.IsEmptyElement);
							break;
						case XmlNodeType.Comment:
							if (htmlWriter != null)
							{
								//if (!elementContentStarted && inlineStyle.Length > 0)
								//{
								//	htmlWriter.WriteAttributeString("style", inlineStyle.ToString());
								//}
								htmlWriter.WriteComment(xamlReader.Value);
							}
							elementContentStarted = true;
							break;
						case XmlNodeType.CDATA:
						case XmlNodeType.Text:
						case XmlNodeType.SignificantWhitespace:
							if (htmlWriter != null)
							{
								//if (!elementContentStarted && inlineStyle.Length > 0)
								//{
								//	htmlWriter.WriteAttributeString("style", inlineStyle.ToString());
								//}
								htmlWriter.WriteString(xamlReader.Value);
							}
							elementContentStarted = true;
							break;
					}
				}

				Debug.Assert(xamlReader.NodeType == XmlNodeType.EndElement);
			}
		}

		/// <summary>
		/// Conberts an element notation of complex property into
		/// </summary>
		/// <param name="xamlReader">
		/// On entry this XmlTextReader must be on Element start tag;
		/// on exit - on EndElement tag.
		/// </param>
		/// <param name="inlineStyle">
		/// StringBuilder containing a value for STYLE attribute.
		/// </param>
		private static void AddComplexProperty(XmlTextReader xamlReader, StringBuilder inlineStyle)
		{
			Debug.Assert(xamlReader.NodeType == XmlNodeType.Element);

			if (inlineStyle != null && xamlReader.Name.EndsWith(".TextDecorations"))
			{
				inlineStyle.Append("text-decoration:underline;");
			}

			// Skip the element representing the complex property
			WriteElementContent(xamlReader, /*htmlWriter:*/null, /*inlineStyle:*/null);
		}

		/// <summary>
		/// Converts a xaml element into an appropriate html element.
		/// </summary>
		/// <param name="xamlReader">
		/// On entry this XmlTextReader must be on Element start tag;
		/// on exit - on EndElement tag.
		/// </param>
		/// <param name="htmlWriter">
		/// May be null, in which case we are skipping xaml content
		/// without producing any html output
		/// </param>
		/// <param name="inlineStyle">
		/// StringBuilder used for collecting css properties for inline STYLE attributes on every level.
		/// </param>
		private static void WriteElement(XmlTextReader xamlReader, XmlTextWriter htmlWriter, StringBuilder inlineStyle)
		{
			Debug.Assert(xamlReader.NodeType == XmlNodeType.Element);

			if (htmlWriter == null)
			{
				// Skipping mode; recurse into the xaml element without any output
				WriteElementContent(xamlReader, /*htmlWriter:*/null, null);
			}
			else
			{
				string htmlElementName = null;
				bool ignoreEmptyTag = false;
				switch (xamlReader.Name)
				{
					case "Run":
					case "Span":
						htmlElementName = "span";
						ignoreEmptyTag = true;
						break;
					case "InlineUIContainer":
						htmlElementName = "span";
						ignoreEmptyTag = true;
						break;
					case "Bold":
						htmlElementName = "b";
						break;
					case "Italic":
						htmlElementName = "i";
						break;
					case "Paragraph":
						htmlElementName = "p";
						break;
					case "BlockUIContainer":
						htmlElementName = "div";
						break;
					case "Section":
						htmlElementName = "div";
						break;
					case "Table":
						htmlElementName = "table";
						break;
					case "TableColumn":
						htmlElementName = "col";
						break;
					case "TableRowGroup":
						htmlElementName = "tbody";
						break;
					case "TableRow":
						htmlElementName = "tr";
						break;
					case "TableCell":
						htmlElementName = "td";
						break;
					case "List":
						string marker = xamlReader.GetAttribute("MarkerStyle");
						if (marker == null || marker == "None" || marker == "Disc" || marker == "Circle" || marker == "Square" || marker == "Box")
						{
							htmlElementName = "ul";
						}
						else
						{
							htmlElementName = "ol";
						}
						break;
					case "ListItem":
						htmlElementName = "li";
						break;
					case "LineBreak":
						htmlElementName = "br";
						break;
					case "Hyperlink":
						htmlElementName = "a";
						break;
					default:
						htmlElementName = null; // Ignore the element
						break;
				}

				if (htmlWriter != null && htmlElementName != null)
				{
					if (!ignoreEmptyTag || xamlReader.AttributeCount > 0)
					{
						htmlWriter.WriteStartElement(htmlElementName.ToLowerInvariant());
						WriteFormattingProperties(xamlReader, htmlWriter, inlineStyle);
					}

					WriteElementContent(xamlReader, htmlWriter, inlineStyle);

					if (!ignoreEmptyTag || xamlReader.AttributeCount > 0)
					{
						htmlWriter.WriteEndElement();
					}
				}
				else
				{
					// Skip this unrecognized xaml element
					WriteElementContent(xamlReader, /*htmlWriter:*/null, null);
				}
			}
		}

		// Reader advance helpers
		// ----------------------

		/// <summary>
		/// Reads several items from xamlReader skipping all non-significant stuff.
		/// </summary>
		/// <param name="xamlReader">
		/// XmlTextReader from tokens are being read.
		/// </param>
		/// <returns>
		/// True if new token is available; false if end of stream reached.
		/// </returns>
		private static bool ReadNextToken(XmlReader xamlReader)
		{
			while (xamlReader.Read())
			{
				Debug.Assert(xamlReader.ReadState == ReadState.Interactive, "Reader is expected to be in Interactive state (" + xamlReader.ReadState + ")");
				switch (xamlReader.NodeType)
				{
					case XmlNodeType.Element:
					case XmlNodeType.EndElement:
					case XmlNodeType.None:
					case XmlNodeType.CDATA:
					case XmlNodeType.Text:
					case XmlNodeType.SignificantWhitespace:
						return true;

					case XmlNodeType.Whitespace:
						if (xamlReader.XmlSpace == XmlSpace.Preserve)
						{
							return true;
						}
						// ignore insignificant whitespace
						break;

					case XmlNodeType.EndEntity:
					case XmlNodeType.EntityReference:
						//  Implement entity reading
						//xamlReader.ResolveEntity();
						//xamlReader.Read();
						//ReadChildNodes( parent, parentBaseUri, xamlReader, positionInfo);
						break; // for now we ignore entities as insignificant stuff

					case XmlNodeType.Comment:
						return true;
					case XmlNodeType.ProcessingInstruction:
					case XmlNodeType.DocumentType:
					case XmlNodeType.XmlDeclaration:
					default:
						// Ignorable stuff
						break;
				}
			}
			return false;
		}

		#endregion Private Methods

		// ---------------------------------------------------------------------
		//
		// Private Fields
		//
		// ---------------------------------------------------------------------

		#region Private Fields

		#endregion Private Fields
	}
}