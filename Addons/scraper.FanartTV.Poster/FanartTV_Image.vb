﻿' ################################################################################
' #                             EMBER MEDIA MANAGER                              #
' ################################################################################
' ################################################################################
' # This file is part of Ember Media Manager.                                    #
' #                                                                              #
' # Ember Media Manager is free software: you can redistribute it and/or modify  #
' # it under the terms of the GNU General Public License as published by         #
' # the Free Software Foundation, either version 3 of the License, or            #
' # (at your option) any later version.                                          #
' #                                                                              #
' # Ember Media Manager is distributed in the hope that it will be useful,       #
' # but WITHOUT ANY WARRANTY; without even the implied warranty of               #
' # MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the                #
' # GNU General Public License for more details.                                 #
' #                                                                              #
' # You should have received a copy of the GNU General Public License            #
' # along with Ember Media Manager.  If not, see <http://www.gnu.org/licenses/>. #
' ################################################################################

Imports System.IO
Imports EmberAPI
Imports RestSharp
Imports WatTmdb
Imports ScraperModule.FANARTTVs
Imports NLog
Imports System.Diagnostics

Public Class FanartTV_Image
    Implements Interfaces.ScraperModule_Image_Movie


#Region "Fields"
    Shared logger As Logger = NLog.LogManager.GetCurrentClassLogger()
    Public Shared ConfigOptions As New Structures.MovieScrapeOptions
    Public Shared ConfigScrapeModifier As New Structures.ScrapeModifier
    Public Shared _AssemblyName As String

    ''' <summary>
    ''' Scraping Here
    ''' </summary>
    ''' <remarks></remarks>
    Private strPrivateAPIKey As String = String.Empty
    Private _MySettings As New sMySettings
    Private _Name As String = "FanartTV_Poster"
    Private _ScraperEnabled As Boolean = False
    Private _setup As frmFanartTVMediaSettingsHolder
    Private _fanartTV As FANARTTVs.Scraper

#End Region 'Fields

#Region "Events"

    Public Event ModuleSettingsChanged() Implements Interfaces.ScraperModule_Image_Movie.ModuleSettingsChanged

    Public Event MovieScraperEvent(ByVal eType As Enums.ScraperEventType_Movie, ByVal Parameter As Object) Implements Interfaces.ScraperModule_Image_Movie.MovieScraperEvent

    Public Event SetupScraperChanged(ByVal name As String, ByVal State As Boolean, ByVal difforder As Integer) Implements Interfaces.ScraperModule_Image_Movie.ScraperSetupChanged

    Public Event SetupNeedsRestart() Implements Interfaces.ScraperModule_Image_Movie.SetupNeedsRestart

    Public Event PostersDownloaded(ByVal Posters As List(Of MediaContainers.Image)) Implements Interfaces.ScraperModule_Image_Movie.PostersDownloaded

    Public Event ProgressUpdated(ByVal iPercent As Integer) Implements Interfaces.ScraperModule_Image_Movie.ProgressUpdated

#End Region 'Events

#Region "Properties"

    ReadOnly Property ModuleName() As String Implements Interfaces.ScraperModule_Image_Movie.ModuleName
        Get
            Return _Name
        End Get
    End Property

    ReadOnly Property ModuleVersion() As String Implements Interfaces.ScraperModule_Image_Movie.ModuleVersion
        Get
            Return System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly.Location).FileVersion.ToString
        End Get
    End Property

    Property ScraperEnabled() As Boolean Implements Interfaces.ScraperModule_Image_Movie.ScraperEnabled
        Get
            Return _ScraperEnabled
        End Get
        Set(ByVal value As Boolean)
            _ScraperEnabled = value
        End Set
    End Property

#End Region 'Properties

#Region "Methods"
    Function QueryPostScraperCapabilities(ByVal cap As Enums.ScraperCapabilities) As Boolean Implements Interfaces.ScraperModule_Image_Movie.QueryScraperCapabilities
        Select Case cap
            Case Enums.ScraperCapabilities.Banner
                Return ConfigScrapeModifier.Banner
            Case Enums.ScraperCapabilities.CharacterArt
                Return ConfigScrapeModifier.CharacterArt
            Case Enums.ScraperCapabilities.ClearArt
                Return ConfigScrapeModifier.ClearArt
            Case Enums.ScraperCapabilities.ClearLogo
                Return ConfigScrapeModifier.ClearLogo
            Case Enums.ScraperCapabilities.DiscArt
                Return ConfigScrapeModifier.DiscArt
            Case Enums.ScraperCapabilities.Fanart
                Return ConfigScrapeModifier.Fanart
            Case Enums.ScraperCapabilities.Landscape
                Return ConfigScrapeModifier.Landscape
            Case Enums.ScraperCapabilities.Poster
                Return ConfigScrapeModifier.Poster
        End Select
        Return False
    End Function

    Private Sub Handle_ModuleSettingsChanged()
        RaiseEvent ModuleSettingsChanged()
    End Sub

    Private Sub Handle_PostModuleSettingsChanged()
        RaiseEvent ModuleSettingsChanged()
    End Sub

    Private Sub Handle_SetupNeedsRestart()
        RaiseEvent SetupNeedsRestart()
    End Sub

    Private Sub Handle_SetupScraperChanged(ByVal state As Boolean, ByVal difforder As Integer)
        ScraperEnabled = state
        RaiseEvent SetupScraperChanged(String.Concat(Me._Name, "Scraper"), state, difforder)
    End Sub

    Sub Init(ByVal sAssemblyName As String) Implements Interfaces.ScraperModule_Image_Movie.Init
        _AssemblyName = sAssemblyName
        LoadSettings()
        'Must be after Load settings to retrieve the correct API key
        _fanartTV = New FANARTTVs.Scraper(_MySettings)
    End Sub

    Function InjectSetupScraper() As Containers.SettingsPanel Implements Interfaces.ScraperModule_Image_Movie.InjectSetupScraper
        Dim Spanel As New Containers.SettingsPanel
        _setup = New frmFanartTVMediaSettingsHolder
        LoadSettings()
        _setup.cbEnabled.Checked = _ScraperEnabled
        _setup.chkScrapePoster.Checked = ConfigScrapeModifier.Poster
        _setup.chkScrapeFanart.Checked = ConfigScrapeModifier.Fanart
        _setup.chkScrapeBanner.Checked = ConfigScrapeModifier.Banner
        _setup.chkScrapeCharacterArt.Checked = ConfigScrapeModifier.CharacterArt
        _setup.chkScrapeClearArt.Checked = ConfigScrapeModifier.ClearArt
        _setup.chkScrapeClearArtOnlyHD.Checked = _MySettings.ClearArtOnlyHD
        _setup.chkScrapeClearLogo.Checked = ConfigScrapeModifier.ClearLogo
        _setup.chkScrapeClearLogoOnlyHD.Checked = _MySettings.ClearLogoOnlyHD
        _setup.chkScrapeDiscArt.Checked = ConfigScrapeModifier.DiscArt
        _setup.chkScrapeLandscape.Checked = ConfigScrapeModifier.Landscape
        _setup.txtFANARTTVApiKey.Text = strPrivateAPIKey
        _setup.cbFANARTTVLanguage.Text = _MySettings.FANARTTVLanguage

        _setup.orderChanged()
        Spanel.Name = String.Concat(Me._Name, "Scraper")
        Spanel.Text = Master.eLang.GetString(788, "FanartTV")
        Spanel.Prefix = "FanartTVMovieMedia_"
        Spanel.Order = 110
        Spanel.Parent = "pnlMovieMedia"
        Spanel.Type = Master.eLang.GetString(36, "Movies")
        Spanel.ImageIndex = If(Me._ScraperEnabled, 9, 10)
        Spanel.Panel = Me._setup.pnlSettings

        AddHandler _setup.SetupScraperChanged, AddressOf Handle_SetupScraperChanged
        AddHandler _setup.ModuleSettingsChanged, AddressOf Handle_PostModuleSettingsChanged
        AddHandler _setup.SetupNeedsRestart, AddressOf Handle_SetupNeedsRestart
        Return Spanel
    End Function

    Sub LoadSettings()
        strPrivateAPIKey = clsAdvancedSettings.GetSetting("FANARTTVApiKey", "")
        _MySettings.FANARTTVApiKey = If(String.IsNullOrEmpty(strPrivateAPIKey), "ea68f9d0847c1b7643813c70cbfc0196", strPrivateAPIKey)
        _MySettings.FANARTTVLanguage = clsAdvancedSettings.GetSetting("FANARTTVLanguage", "en")
        _MySettings.ClearArtOnlyHD = clsAdvancedSettings.GetBooleanSetting("ClearArtOnlyHD", False)
        _MySettings.ClearLogoOnlyHD = clsAdvancedSettings.GetBooleanSetting("ClearLogoOnlyHD", False)

        ConfigScrapeModifier.Poster = clsAdvancedSettings.GetBooleanSetting("DoPoster", True)
        ConfigScrapeModifier.Fanart = clsAdvancedSettings.GetBooleanSetting("DoFanart", True)
        ConfigScrapeModifier.Banner = clsAdvancedSettings.GetBooleanSetting("DoBanner", True)
        ConfigScrapeModifier.CharacterArt = clsAdvancedSettings.GetBooleanSetting("DoCharacterArt", True)
        ConfigScrapeModifier.ClearArt = clsAdvancedSettings.GetBooleanSetting("DoClearArt", True)
        ConfigScrapeModifier.ClearLogo = clsAdvancedSettings.GetBooleanSetting("DoClearLogo", True)
        ConfigScrapeModifier.DiscArt = clsAdvancedSettings.GetBooleanSetting("DoDiscArt", True)
        ConfigScrapeModifier.Landscape = clsAdvancedSettings.GetBooleanSetting("DoLandscape", True)
        ConfigScrapeModifier.EThumbs = ConfigScrapeModifier.Fanart
        ConfigScrapeModifier.EFanarts = ConfigScrapeModifier.Fanart
    End Sub

    Sub SaveSettings()
        Using settings = New clsAdvancedSettings()
            settings.SetBooleanSetting("ClearArtOnlyHD", _MySettings.ClearArtOnlyHD)
            settings.SetBooleanSetting("ClearLogoOnlyHD", _MySettings.ClearLogoOnlyHD)
            settings.SetBooleanSetting("DoPoster", ConfigScrapeModifier.Poster)
            settings.SetBooleanSetting("DoFanart", ConfigScrapeModifier.Fanart)
            settings.SetBooleanSetting("DoBanner", ConfigScrapeModifier.Banner)
            settings.SetBooleanSetting("DoCharacterArt", ConfigScrapeModifier.CharacterArt)
            settings.SetBooleanSetting("DoClearArt", ConfigScrapeModifier.ClearArt)
            settings.SetBooleanSetting("DoClearLogo", ConfigScrapeModifier.ClearLogo)
            settings.SetBooleanSetting("DoDiscArt", ConfigScrapeModifier.DiscArt)
            settings.SetBooleanSetting("DoLandscape", ConfigScrapeModifier.Landscape)
            settings.SetSetting("FANARTTVApiKey", _setup.txtFANARTTVApiKey.Text)
            settings.SetSetting("FANARTTVLanguage", _MySettings.FANARTTVLanguage)
        End Using
    End Sub

    Sub SaveSetupScraper(ByVal DoDispose As Boolean) Implements Interfaces.ScraperModule_Image_Movie.SaveSetupScraper
        _MySettings.FANARTTVLanguage = _setup.cbFANARTTVLanguage.Text
        _MySettings.ClearArtOnlyHD = _setup.chkScrapeClearArtOnlyHD.Checked
        _MySettings.ClearLogoOnlyHD = _setup.chkScrapeClearLogoOnlyHD.Checked
        ConfigScrapeModifier.Poster = _setup.chkScrapePoster.Checked
        ConfigScrapeModifier.Fanart = _setup.chkScrapeFanart.Checked
        ConfigScrapeModifier.Banner = _setup.chkScrapeBanner.Checked
        ConfigScrapeModifier.CharacterArt = _setup.chkScrapeCharacterArt.Checked
        ConfigScrapeModifier.ClearArt = _setup.chkScrapeClearArt.Checked
        ConfigScrapeModifier.ClearLogo = _setup.chkScrapeClearLogo.Checked
        ConfigScrapeModifier.DiscArt = _setup.chkScrapeDiscArt.Checked
        ConfigScrapeModifier.Landscape = _setup.chkScrapeLandscape.Checked
        SaveSettings()
        'ModulesManager.Instance.SaveSettings()
        If DoDispose Then
            RemoveHandler _setup.SetupScraperChanged, AddressOf Handle_SetupScraperChanged
            RemoveHandler _setup.ModuleSettingsChanged, AddressOf Handle_ModuleSettingsChanged
            RemoveHandler _setup.SetupNeedsRestart, AddressOf Handle_SetupNeedsRestart
            _setup.Dispose()
        End If
    End Sub
    Function Scraper(ByRef DBMovie As Structures.DBMovie, ByVal Type As Enums.ScraperCapabilities, ByRef ImageList As List(Of MediaContainers.Image)) As Interfaces.ModuleResult Implements Interfaces.ScraperModule_Image_Movie.Scraper
        logger.Trace("Started scrape")

        LoadSettings()

        ImageList = _fanartTV.GetFANARTTVImages(DBMovie.Movie.ID, Type)

        logger.Trace(New StackFrame().GetMethod().Name, "Finished scrape")
        Return New Interfaces.ModuleResult With {.breakChain = False}
    End Function

    Function Scraper(ByRef DBMovieset As Structures.DBMovieSet, ByVal Type As Enums.ScraperCapabilities, ByRef ImageList As List(Of MediaContainers.Image)) As Interfaces.ModuleResult Implements Interfaces.ScraperModule_Image_Movie.Scraper
        logger.Trace("Started scrape")

        LoadSettings()

        ImageList = _fanartTV.GetFANARTTVImages(DBMovieset.MovieSet.ID, Type)

        logger.Trace("Finished scrape")
        Return New Interfaces.ModuleResult With {.breakChain = False}
    End Function

    Public Sub ScraperOrderChanged() Implements EmberAPI.Interfaces.ScraperModule_Image_Movie.ScraperOrderChanged
        _setup.orderChanged()
    End Sub

#End Region 'Methods

#Region "Nested Types"

    Structure sMySettings

#Region "Fields"
        Dim ClearArtOnlyHD As Boolean
        Dim ClearLogoOnlyHD As Boolean
        Dim FANARTTVApiKey As String
        Dim FANARTTVLanguage As String
#End Region 'Fields

    End Structure

#End Region 'Nested Types

End Class