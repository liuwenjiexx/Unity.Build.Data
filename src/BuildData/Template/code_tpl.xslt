<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:msxsl="urn:schemas-microsoft-com:xslt" exclude-result-prefixes="msxsl"   xmlns:tpl="urn:templates"
>
  <xsl:output method="text" indent="yes"/>
  <xsl:key match="Type" use="@Namespace" name="NamespaceKey"/>

  <xsl:template match="/">
    <!--<xsl:value-of select ="tpl:Set('OutputPath',@Output)"/>-->

    <xsl:text disable-output-escaping="no">
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
</xsl:text>
    <xsl:call-template name="IndexerBaseClass"/>
    <xsl:apply-templates />
  </xsl:template>

  <xsl:template match="Type">

    <xsl:if test="@Namespace != ''">
      <xsl:text>namespace </xsl:text>
      <xsl:value-of select="@Namespace"/>
      <xsl:text>
{

</xsl:text>
    </xsl:if>


    <xsl:choose>
      <xsl:when test="@Type='Struct'">
        <xsl:text>
    [Serializable]
    public struct </xsl:text>
      </xsl:when>
      <xsl:when test="@Type='Enum'">
        <xsl:text>
    public enum </xsl:text>
      </xsl:when>
      <xsl:otherwise>
        <xsl:text>
        [Serializable]
        public class </xsl:text>
      </xsl:otherwise>
    </xsl:choose>

    <xsl:value-of select="@Name"/>
    <xsl:text>
    {
</xsl:text>
    <xsl:apply-templates />


    <xsl:text>
    }</xsl:text>


    <xsl:for-each select="Field[@Key='True']">
      <xsl:call-template name="IndexerClass"/>
    </xsl:for-each>

    <xsl:if test="@Namespace != ''">
      <xsl:text>
}</xsl:text>
    </xsl:if>
  </xsl:template>

  <xsl:template name="Member">
    <xsl:if test="@Description != '' ">
      <xsl:text>
        /// &lt;summary&gt;
        /// </xsl:text>
      <xsl:value-of select="@Description"/>
      <xsl:text>
        /// &lt;/summary&gt;</xsl:text>
    </xsl:if>
    <xsl:choose >
      <xsl:when test="../@Type='Enum'">
        <xsl:text>
        </xsl:text>
        <xsl:value-of select="@Name"/>
        <xsl:text> = </xsl:text>
        <xsl:value-of select="@Value"/>
        <xsl:text>,</xsl:text>
      </xsl:when>
      <xsl:otherwise>
        <xsl:text>
        public </xsl:text>
        <xsl:value-of select="@Type"/>
        <xsl:text> </xsl:text>
        <xsl:value-of select="@Name"/>
      </xsl:otherwise>
    </xsl:choose>

  </xsl:template>

  <!-- Field -->
  <xsl:template match="Field">
    <xsl:call-template name="Member"/>
    <xsl:text>;
</xsl:text>
  </xsl:template>
  <!-- Field -->

  <!-- Property -->
  <xsl:template match="Property">
    <xsl:call-template name="Member"/>
    <xsl:text>{
    get;
    set;
    }</xsl:text>
  </xsl:template>
  <!-- Property -->

  <!-- Enum -->
  <xsl:template match="Enum">
    <xsl:call-template name="Member"/>
  </xsl:template>
  <!-- Enum -->

  <xsl:template name="IndexerBaseClass">
    public abstract class _IndexerBase&lt;TKey, TValue&gt;
    {
    private Dictionary&lt;TKey, TValue&gt;
    items = new Dictionary&lt;TKey, TValue&gt;
    ();
    public List&lt;TValue&gt;
    list = new List&lt;TValue&gt;
    ();

    public Dictionary&lt;TKey, TValue&gt; Items
    {
    get { return items; }
    }

    public abstract void Initialize(IEnumerable items);

    public virtual TValue Get(TKey key)
    {
    TValue value;
    if(!items.TryGetValue(key, out value))
    value=default(TValue);
    return value;
    }
    }
  </xsl:template>

  <xsl:template name="IndexerClass">
    <!--<xsl:text>
        public static Dictionary&lt;</xsl:text>
    <xsl:value-of select="@Type"/>
    <xsl:text>, </xsl:text>
    <xsl:value-of select="../@Name"/>&gt; _DICT;-->
    <xsl:text>
    public class </xsl:text><xsl:call-template name="IndexerName"/>:_IndexerBase&lt;<xsl:value-of select="@Type"/>, <xsl:value-of select="../@Name"/>&gt;
    {

    private static <xsl:call-template name="IndexerName"/><xsl:text> instance;</xsl:text>

    public override void Initialize<xsl:text>(IEnumerable items)
    {
</xsl:text>
    foreach (<xsl:value-of select="../@Name"/> o in items)
    {
    this.Items[o.<xsl:value-of select="@Name"/>] = o;
    this.list.Add(o);
    }
    }

    public static <xsl:call-template name="IndexerName"/><xsl:text> Instance
    {</xsl:text>
    get
    {
    if (instance == null)
    instance = new <xsl:call-template name="IndexerName"/><xsl:text>();</xsl:text>
    return instance;
    }
    }

    }
  </xsl:template>


  <xsl:template name="IndexerName">
    <xsl:value-of select="../@Name"/>
    <xsl:text>Indexer</xsl:text>
  </xsl:template>

</xsl:stylesheet>
