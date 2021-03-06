/*
 * DO NOT ALTER OR REMOVE COPYRIGHT NOTICES OR THIS HEADER.
 * 
 * Copyright (c) 2009-2010 Sun Microsystems Inc. All Rights Reserved
 * 
 * The contents of this file are subject to the terms
 * of the Common Development and Distribution License
 * (the License). You may not use this file except in
 * compliance with the License.
 * 
 * You can obtain a copy of the License at
 * https://opensso.dev.java.net/public/CDDLv1.0.html or
 * opensso/legal/CDDLv1.0.txt
 * See the License for the specific language governing
 * permission and limitations under the License.
 * 
 * When distributing Covered Code, include this CDDL
 * Header Notice in each file and include the License file
 * at opensso/legal/CDDLv1.0.txt.
 * If applicable, add the following below the CDDL Header,
 * with the fields enclosed by brackets [] replaced by
 * your own identifying information:
 * "Portions Copyrighted [year] [name of copyright owner]"
 * 
 * $Id: LogoutRequest.cs,v 1.2 2010/01/19 18:23:09 ggennaro Exp $
 */

using System;
using System.Collections.Specialized;
using System.Globalization;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using Sun.Identity.Properties;
using Sun.Identity.Saml2.Exceptions;

namespace Sun.Identity.Saml2
{
	/// <summary>
	/// SAMLv2 LogoutRequest object constructed from either a response obtained
	/// from an Identity Provider for the hosted Service Provider or generated
	/// by this Service Provider to be sent to a desired Identity Provider.
	/// </summary>
	public class LogoutRequest
	{
		#region Members

		/// <summary>
		/// Namespace Manager for this logout request.
		/// </summary>
		private readonly XmlNamespaceManager m_nsMgr;

		/// <summary>
		/// XML representation of the logout request.
		/// </summary>
		private readonly XmlDocument m_xml;

		#endregion

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the LogoutRequest class.
		/// </summary>
		/// <param name="samlRequest">Decoded SAMLv2 Logout Request</param>
		public LogoutRequest(string samlRequest)
		{
			try
			{
			    m_xml = new XmlDocument {PreserveWhitespace = true};

			    m_nsMgr = new XmlNamespaceManager(m_xml.NameTable);
				m_nsMgr.AddNamespace("ds", SignedXml.XmlDsigNamespaceUrl);
				m_nsMgr.AddNamespace("saml", "urn:oasis:names:tc:SAML:2.0:assertion");
				m_nsMgr.AddNamespace("samlp", "urn:oasis:names:tc:SAML:2.0:protocol");

				m_xml.LoadXml(samlRequest);
			}
			catch (ArgumentNullException ane)
			{
				throw new Saml2Exception(Resources.LogoutRequestNullArgument, ane);
			}
			catch (XmlException xe)
			{
				throw new Saml2Exception(Resources.LogoutRequestXmlException, xe);
			}
		}

	    /// <summary>
	    /// Initializes a new instance of the LogoutRequest class.
	    /// </summary>
	    /// <param name="identityProvider">
	    /// IdentityProvider of the LogoutRequest
	    /// </param>
	    /// <param name="serviceProvider">
	    /// ServiceProvider of the LogoutRequest
	    /// </param>
	    /// <param name="parameters">
	    /// NameValueCollection of varying parameters for use in the 
	    /// construction of the LogoutRequest.
	    /// </param>
	    public LogoutRequest(
			IIdentityProvider identityProvider,
			IServiceProvider serviceProvider,
			NameValueCollection parameters)
		{
			try
			{
			    m_xml = new XmlDocument {PreserveWhitespace = true};

			    m_nsMgr = new XmlNamespaceManager(m_xml.NameTable);
				m_nsMgr.AddNamespace("ds", SignedXml.XmlDsigNamespaceUrl);
				m_nsMgr.AddNamespace("saml", "urn:oasis:names:tc:SAML:2.0:assertion");
				m_nsMgr.AddNamespace("samlp", "urn:oasis:names:tc:SAML:2.0:protocol");

				string sessionIndex = null;
				string subjectNameId = null;
				string binding = null;
				string destination = null;

				if (parameters != null)
				{
					sessionIndex = parameters[Saml2Constants.SessionIndex];
					subjectNameId = parameters[Saml2Constants.SubjectNameId];
					binding = parameters[Saml2Constants.Binding];
					destination = parameters[Saml2Constants.Destination];
				}

				if (string.IsNullOrEmpty(sessionIndex))
				{
					throw new Saml2Exception(Resources.LogoutRequestSessionIndexNotDefined);
				}
				if (string.IsNullOrEmpty(subjectNameId))
				{
					throw new Saml2Exception(Resources.LogoutRequestSubjectNameIdNotDefined);
				}
				if (serviceProvider == null)
				{
					throw new Saml2Exception(Resources.LogoutRequestServiceProviderIsNull);
				}
				if (identityProvider == null)
				{
					throw new Saml2Exception(Resources.LogoutRequestIdentityProviderIsNull);
				}

				if (string.IsNullOrEmpty(destination))
				{
					destination = identityProvider.GetSingleLogoutServiceLocation(binding);

					if (string.IsNullOrEmpty(destination))
					{
						// default with HttpRedirect
						destination = identityProvider.GetSingleLogoutServiceLocation(Saml2Constants.HttpRedirectProtocolBinding);
					}
				}

				var rawXml = new StringBuilder();
				rawXml.Append("<samlp:LogoutRequest xmlns:samlp=\"urn:oasis:names:tc:SAML:2.0:protocol\"");
                rawXml.Append(" ID=\"" + Saml2Utils.GenerateId() + "\"");
				rawXml.Append(" Version=\"2.0\"");
                rawXml.Append(" IssueInstant=\"" + Saml2Utils.GenerateIssueInstant() + "\"");

				if (!String.IsNullOrEmpty(destination))
				{
					rawXml.Append(" Destination=\"" + destination + "\"");
				}

				rawXml.Append(" >");
                rawXml.Append(" <saml:Issuer xmlns:saml=\"urn:oasis:names:tc:SAML:2.0:assertion\">" + serviceProvider.EntityId +
                              "</saml:Issuer>");
                rawXml.Append(" <saml:NameID xmlns:saml=\"urn:oasis:names:tc:SAML:2.0:assertion\"");
				//rawXml.Append("  Format=\"urn:oasis:names:tc:SAML:2.0:nameid-format:transient\"");
				//rawXml.Append("  NameQualifier=\"" + identityProvider.EntityId + "\">" + subjectNameId + "</saml:NameID> ");
				rawXml.Append("  >" + subjectNameId + "</saml:NameID> ");
                rawXml.Append(" <samlp:SessionIndex xmlns:samlp=\"urn:oasis:names:tc:SAML:2.0:protocol\">" + sessionIndex +
				              "</samlp:SessionIndex>");
				rawXml.Append("</samlp:LogoutRequest>");

				m_xml.LoadXml(rawXml.ToString());
			}
			catch (ArgumentNullException ane)
			{
				throw new Saml2Exception(Resources.LogoutRequestNullArgument, ane);
			}
			catch (XmlException xe)
			{
				throw new Saml2Exception(Resources.LogoutRequestXmlException, xe);
			}
		}

		#endregion

		#region Properties

		/// <summary>
		/// Gets the extracted "NotOnOrAfter" from the logout request,
		/// otherwise DateTime.MinValue since this is an optional attribute.
		/// </summary>
		public DateTime NotOnOrAfter
		{
			get
			{
                const string xpath = "/samlp:LogoutRequest";
			    var value = Saml2Utils.TryGetAttributeValue(m_xml, m_nsMgr, xpath, "NotOnOrAfter");
			    return string.IsNullOrEmpty(value)
			        ? DateTime.MinValue
			        : DateTime.Parse(value, CultureInfo.InvariantCulture);
			}
		}

        /// <summary>
        /// Gets the ID attribute value of the logout request.
		/// Throws if none provided.
        /// </summary>
        public string Id
		{
			get
			{
                const string xpath = "/samlp:LogoutRequest";
			    return Saml2Utils.RequireAttributeValue(m_xml, m_nsMgr, xpath, "ID");
			}
		}

        /// <summary>
        /// Gets the name of the issuer of the logout request.
        /// Throws if none provided.
        /// </summary>
        public string Issuer
		{
			get
			{
                const string xpath = "/samlp:LogoutRequest/saml:Issuer";
                return Saml2Utils.RequireNodeText(m_xml, m_nsMgr, xpath);
			}
		}

        /// <summary>
        /// Gets the XML representation of the received logout request.
        /// <c>null</c> if none provided.
        /// </summary>
        public IXPathNavigable XmlDom => m_xml;

        /// <summary>
        /// Gets the signature of the logout request as an XML element.
        /// <c>null</c> if none provided.
        /// </summary>
        public IXPathNavigable XmlSignature
		{
			get
			{
				const string xpath = "/samlp:LogoutRequest/ds:Signature";
                return Saml2Utils.TryGetNode(m_xml, m_nsMgr, xpath);
            }
		}

		#endregion

		#region Methods

		#endregion
	}
}