<?xml version="1.0" encoding="UTF-8" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:variable name="ReportName" select="Report/Converter"/>
  <xsl:variable name="ReportType" select="Report/Type"/>
  <xsl:variable name="HatConReport" select="$ReportName = 'VSSConverter' or $ReportName = 'SDConverter'"/>
  <xsl:variable name="CurConReport" select="$ReportName = 'CQConverter' or $ReportName = 'PSConverter'"/>
  <xsl:template match="Report">
    <html>
      <head>
        <META http-equiv="Content-Type" content="text/html; charset=UTF-16"/>
        <META HTTP-EQUIV="Content-Type" content="text/html; charset=utf-8"/>
        <link rel="stylesheet" href="_MigrationReport_Files\UpgradeReport.css"></link>
        <title>
          <xsl:value-of select="Title"/>
        </title>
        <script language="Javascript">
          function outliner()
          {
            oMe = window.event.srcElement

            //get child element
            var child = document.all[event.srcElement.getAttribute("child",false)];

            //if child element exists, expand or collapse it.
            if (null != child)
              child.className = child.className == "collapsed" ? "expanded" : "collapsed";
          }

          function changepic(uMe)
          {
            if(uMe == null)
            {
              return;
            }

            var check = uMe.src.toLowerCase();
            if (check.lastIndexOf("upgradereport_plus.gif") != -1)
            {
              uMe.src = "_MigrationReport_Files/UpgradeReport_Minus.gif"
            }
            else
            {
              uMe.src = "_MigrationReport_Files/UpgradeReport_Plus.gif"
            }
          }
        </script>

      </head>
      <body topmargin="0" leftmargin="5" rightmargin="0" onclick="outliner();">
        <h1>
          <xsl:value-of select="Title"/>
        </h1>

        <!-- Summary Section -->
        <h2>
          <IMG style="background-color:transparent;" alt="expand/collapse section" align="center" class="expandable"  onclick="changepic(this)" 
          src="_MigrationReport_Files/UpgradeReport_Minus.gif" width="21" child="srcSummary"></IMG>
          Summary
        </h2>
        <div class="expanded" ID="srcSummary">

          <table width="100%" border="0" cellpadding="1" cellspacing="1">
            <tr>
              <td width="15"></td>
              <td>
                <span class="overview">
                  <b>Status</b>
                </span>
              </td>
              <td>
                <span class="overview">
                  <xsl:value-of select="Summary/Status"/> {
                  <xsl:if test="Statistics/HasCriticalError = 'true'">
                    <font color="red">
                      1
                      <a href="#CriticalErrorSection"  style="color: red;">Critical Error</a>  |
                    </font>
                  </xsl:if>
                  <font color="blue">
                    <xsl:value-of select="concat(Statistics/NumberOfErrors, ' ')" />
                    <a href="#MinorErrorSection"  style="color: blue;">Errors</a>  |
                  </font>
                  <font color="blue">
                    <xsl:value-of select="concat(Statistics/NumberOfWarnings, ' ')"/>
                    <a href="#WarningSection" style="color: blue;">Warnings</a>
                  </font> } 
                  <xsl:if test="$ReportName='VSSConverter' and Type='PostMigration'">
                    <a href="http://go.microsoft.com/fwlink/?linkid=64084" style="color: blue;">Click here for post migration steps</a>
                  </xsl:if>
                </span>
              </td>
            </tr>

            <xsl:if test="$ReportName='VSSConverter' and Type='PostMigration'">
              <tr>
                <td width="15"></td>
                <td>
                  <span class="overview">
                    <b>Type of Migration</b>
                  </span>
                </td>
                <td>
                  <span class="overview">
                    <xsl:choose>
                      <xsl:when test="Summary/TypeOfMigration = 'Full'">
                        Full
                      </xsl:when>
                      <xsl:otherwise>
                        Incremental
                      </xsl:otherwise>
                    </xsl:choose>
                  </span>
                </td>
              </tr>
            </xsl:if>

            <tr>
              <td width="5"></td>
              <td>
                <span class="overview">
                  <xsl:choose>
                    <xsl:when test="$HatConReport">
                      <b>Source Control</b>
                    </xsl:when>
                    <xsl:when test="$CurConReport">
                      <b>Source Work Item Tracking System</b>
                    </xsl:when>
                  </xsl:choose>
                </span>
              </td>
              <td>
                <span class="overview">
                  <xsl:choose>
                    <xsl:when test="$ReportName='VSSConverter'">
                      Visual SourceSafe
                    </xsl:when>
                    <xsl:when test="$ReportName='SDConverter'">
                      Source Depot
                    </xsl:when>
                    <xsl:when test="$ReportName='CQConverter'">
                      ClearQuest
                    </xsl:when>
                    <xsl:when test="$ReportName='PSConverter'">
                      Product Studio
                    </xsl:when>
                    <xsl:otherwise>
                      <xsl:value-of select="SourceSystem"/>
                    </xsl:otherwise>
                  </xsl:choose>
                </span>
              </td>
            </tr>
            <tr>
              <td width="5"></td>
              <td>
                <span class="overview">
                  <xsl:choose>
                    <xsl:when test="$ReportName = 'VSSConverter'">
                      <b>VSS Location</b>
                    </xsl:when>
                    <xsl:when test="$ReportName = 'SDConverter'">
                      <b>Source Depot</b>
                    </xsl:when>
                  </xsl:choose>
                </span>
              </td>
              <td>
                <span class="overview">
                  <xsl:if test="$HatConReport">
                    <xsl:value-of select="Summary/SourceAndDestination/Repository/@Source"/>
                  </xsl:if>
                </span>
              </td>
            </tr>
            <xsl:if test="Type='PostMigration' or (Type='PreMigration' and $CurConReport)">
              <tr>
                <td width="5"></td>
                <td>
                  <span class="overview">
                    <b>Team Foundation Server</b>
                  </span>
                </td>
                <td>
                  <span class="overview">
                    <xsl:if test="$HatConReport">
                      <xsl:value-of select="Summary/SourceAndDestination/Repository/@Destination"/>
                    </xsl:if>
                    <xsl:if test="$CurConReport">
                      <xsl:value-of select="Summary/SourceAndDestination/SummaryTarget/Uri"/>
                    </xsl:if>
                  </span>
                </td>
              </tr>
            </xsl:if>

            <xsl:if test="$ReportName!='VSSConverter' or Type='PostMigration'">
              <tr>
                <td width="5"></td>
                <td>
                  <span class="overview">
                    <xsl:choose>
                      <xsl:when test="$ReportName='VSSConverter'">
                        <b>Actions Migrated</b>
                      </xsl:when>
                      <xsl:when test="$ReportName='SDConverter'">
                        <b>Changesets
                          <xsl:if test="$ReportType='PreMigration'"> Analyzed of Total</xsl:if>
                          <xsl:if test="$ReportType='PostMigration'"> Migrated of Total</xsl:if>
                        </b>
                      </xsl:when>
                      <xsl:when test="$ReportName='CQConverter' or $ReportName='PSConverter'">
                        <b>
                          <xsl:if test="$ReportType='PreMigration'">Work Item Types Analyzed</xsl:if>
                          <xsl:if test="$ReportType='PostMigration'">Work Items Migrated</xsl:if>
                        </b>
                      </xsl:when>
                    </xsl:choose>
                  </span>
                </td>
                <td>
                  <span class="overview">
                    <xsl:value-of select="Statistics/NumberOfItems"/>
                    <xsl:if test="$ReportName='SDConverter'">
                      of
                      <xsl:value-of select="Statistics/StatisicsDetails/NumberOfActions"/>
                    </xsl:if>
                  </span>
                </td>
              </tr>
            </xsl:if>

            <xsl:if test="$ReportName='VSSConverter'and Summary/TypeOfMigration!='Incremental'">
              <tr>
                <td width="5"></td>
                <td>
                  <span class="overview">
                    <b>Files and Folders
                      <xsl:if test="$ReportType='PreMigration'"> Analyzed</xsl:if>
                      <xsl:if test="$ReportType='PostMigration'"> Migrated</xsl:if>
                    </b>
                  </span>
                </td>
                <td>
                  <span class="overview">
                    <xsl:value-of select="Statistics/StatisicsDetails/NumberOfActions"/>
                  </span>
                </td>
              </tr>
            </xsl:if>

            <xsl:if test="Summary/TypeOfMigration='Incremental'">
              <tr>
                <td width="5"></td>
                <td>
                  <span class="overview">
                    <b>Interval of VSS History migrated</b>
                  </span>
                </td>
                <td>
                  <span class="overview">
                    <xsl:value-of select="Summary/SourceActionStartTime"/> To <xsl:value-of select="Summary/SourceActionEndTime"/>
                  </span>
                </td>
              </tr>
            </xsl:if>

            <xsl:if test="$ReportName='VSSConverter' and Type='PostMigration'">
              <tr>
                <td width="5"></td>
                <td>
                  <span class="overview">
                    <b>Verified &amp; Fixed Pinned Versions</b>
                  </span>
                </td>
                <td>
                  <span class="overview">
                    <xsl:value-of select="Summary/SourceAndDestination/MatchSummary/@PinVersion"/>
                  </span>
                </td>
              </tr>
              <tr>
                <td width="5"></td>
                <td>
                  <span class="overview">
                    <b>Verified &amp; Fixed Latest Tip Versions</b>
                  </span>
                </td>
                <td>
                  <span class="overview">
                    <xsl:value-of select="Summary/SourceAndDestination/MatchSummary/@TipVersion"/>
                  </span>
                </td>
              </tr>
            </xsl:if>

            <tr>
              <td width="5"></td>
              <td>
                <span class="overview">
                  <b>Start Time</b>
                </span>
              </td>
              <td>
                <span class="overview">
                  <xsl:value-of select="Summary/StartTime"/>
                </span>
              </td>
            </tr>
            <tr>
              <td width="5"></td>
              <td>
                <span class="overview">
                  <b>End Time</b>
                </span>
              </td>
              <td>
                <span class="overview">
                  <xsl:value-of select="Summary/EndTime"/>
                </span>
              </td>
            </tr>
            <tr>
              <td width="5"></td>
              <td>
                <span class="overview">
                  <b>Total Time</b>
                </span>
              </td>
              <td>
                <span class="overview">
                  <xsl:value-of select="Summary/TotalTime"/>
                </span>
              </td>
            </tr>

            <tr>
              <td width="5"></td>
              <td>
                <span class="overview">
                  <b>
                    <xsl:if test="$ReportType='PreMigration'">Analysis </xsl:if>
                    <xsl:if test="$ReportType='PostMigration'">Migration </xsl:if>
                    done by
                  </b>
                </span>
              </td>
              <td>
                <span class="overview">
                  <xsl:value-of select="RunBy"/>
                </span>
              </td>
            </tr>

            <xsl:if test="$CurConReport and $ReportType='PreMigration'">
              <tr>
                <td width="5"></td>
                <td>
                  <span class="overview">
                    <b>Work Item Types Analyzed</b>
                  </span>
                </td>
                <td>
                  <span class="overview">
                    <xsl:for-each select="Summary/SourceAndDestination/WorkItemTypes/WorkItemType">
                      <xsl:if test="position() != 1">, </xsl:if>
                      <xsl:value-of select="@From"/>
                    </xsl:for-each>
                  </span>
                </td>
              </tr>
            </xsl:if>

            <tr/>
            <tr/>

            <xsl:if test="count(Statistics/StatisicsDetails/PerWorkItemType/*) &gt; 0">
              <xsl:if test="$CurConReport and $ReportType='PostMigration'">
                <tr>
                  <td width="5"></td>
                  <td>
                    <span class="overview">
                      <b>Work Item Types Migrated</b>
                    </span>
                  </td>
                  <td></td>
                </tr>
                <tr width = "100%">
                  <td width="15" />
                  <td colspan="2">
                    <!-- get all work item types migrated in tabular data -->
                    <table cellspacing="1" cellpadding="1" class="infotable">
                      <tr>
                        <td width="20%" class="header">Source Work Item Type</td>
                        <td width="20%" class="header">Team Foundation Work Item Type</td>
                        <td class="header">Passed</td>
                        <td class="header">Failed</td>
                        <td class="header">Skipped</td>
                      </tr>
                      <xsl:for-each select="Statistics/StatisicsDetails/PerWorkItemType/WorkItem">
                        <tr>
                          <td class="content">
                            <xsl:value-of select="@From" />
                          </td>
                          <td class="content">
                            <xsl:value-of select="@To" />
                          </td>
                          <td class="content">
                            <xsl:value-of select="@Pass" />
                          </td>
                          <td class="content">
                            <xsl:value-of select="@Fail" />
                          </td>
                          <td class="content">
                            <xsl:value-of select="@Skipped" />
                          </td>
                        </tr>
                      </xsl:for-each>
                    </table>
                  </td>
                </tr>
              </xsl:if>
            </xsl:if>

            <xsl:if test ="$HatConReport and count(Summary/SourceAndDestination/Projects/*) &gt; 0">
              <tr>
                <td width="1"></td>
                <td colspan="2">

                  <span style="margin-left:0px;font-size:12px" class="overview">
                    <b>Folders
                      <xsl:if test="$ReportType='PreMigration'"> Analyzed</xsl:if>
                      <xsl:if test="$ReportType='PostMigration'"> Migrated</xsl:if>
                    </b>
                  </span>

                  <table cellspacing="1" cellpadding="1" class="infotable">
                    <tr>
                      <td class="header" width="50%">
                        <xsl:choose>
                          <xsl:when test="$ReportName = 'VSSConverter'">
                            VSS Folder
                          </xsl:when>
                          <xsl:when test="$ReportName = 'SDConverter'">
                            Source Depot Folder
                          </xsl:when>
                        </xsl:choose>
                      </td>
                      <xsl:if test="$ReportType='PostMigration'">
                        <td class="header" width="50%">Team Foundation Folder</td>
                      </xsl:if>
                    </tr>
                    <xsl:for-each select="Summary/SourceAndDestination/Projects/SourceControlConverterConverterSpecificSettingProject">
                      <tr>
                        <td class="content">
                          <xsl:value-of select="@Source"/>
                        </td>
                        <xsl:if test="$ReportType='PostMigration'">
                          <td class="content">
                            <xsl:value-of select="@Destination"/>
                          </td>
                        </xsl:if>
                      </tr>
                    </xsl:for-each>
                  </table>
                </td>
              </tr>
            </xsl:if>
          </table>
        </div>

        <!-- Critical Errors Section -->
        <xsl:if test="count(Issues/Issue[@Type='Critical'])>0">
          <a name="CriticalErrorSection">
            <h2>
              <IMG name="imgCriticalErrors" alt="expand/collapse section" align="center" class="expandable" onclick="changepic(this)" src="_MigrationReport_Files/UpgradeReport_Minus.gif" width="21" child="srcCriticalErrors"></IMG>
              Critical Errors
            </h2>
          </a>
          <div class="expanded" id="srcCriticalErrors">
            <table width="100%">
              <tr>
                <td width="15"></td>
                <td>
                  <table cellspacing="1" cellpadding="1" class="infotable">
                    <tr>
                      <td style="word-break:break-all" class="content">
                        <font color="red">
                          <xsl:choose>
                            <xsl:when test="$ReportType = 'PostMigration'">Migration </xsl:when>
                            <xsl:when test="$ReportType = 'PreMigration'">Analysis </xsl:when>
                          </xsl:choose>
                          failed due to <xsl:value-of select="concat('&quot;', Issues/Issue[@Type='Critical']/Message, '&quot; ')"/>
                        </font>
                        <a href="http://go.microsoft.com/fwlink/?linkid=55081" style="color: blue;">Click here for more information</a>
                      </td>
                    </tr>
                  </table>
                </td>
              </tr>
            </table>
          </div>
        </xsl:if>


        <!-- Minor Errors Section -->
        <xsl:if test="count(Issues/Issue[@Type='Error'])>0">
          <a name="MinorErrorSection">
            <h2>
              <IMG name="imgMinorErrors"  alt="expand/collapse section" align="center" class="expandable" onclick="changepic(this)" src="_MigrationReport_Files/UpgradeReport_Minus.gif" width="21" child="srcErrors"></IMG>
              Errors
            </h2>
          </a>
          <div class="expanded" id="srcErrors">
            <xsl:if test="$HatConReport">
              <span style="margin-left:20px;font-size:11px;width:'100%'" class="overview">
                The error section is about loss of data or information during migration. Errors do not indicate that migration failed, they only alert you to information that the converter was not able to migrate, for example, specific versions of a file or folder, so you can migrate these files manually. The error message gives a brief description of the error and a link to an MSDN document with more details about the error.
              </span>

              <xsl:if test="count(Issues/Issue[@IssueID='PinVersionMatchError'])>0">
                <table width="100%">
                  <tr>
                    <td width="15"></td>
                    <td>
                      <span style="margin-left:0px;font-size:12px;width:'100%'" class="overview">
                        <b>Failures from verification of pinned versions</b>
                      </span>
                      <table cellspacing="1" cellpadding="1" class="infotable">
                        <tr>
                          <td width="100%" class="header">File</td>
                        </tr>
                        <xsl:for-each select="Issues/Issue[@IssueID='PinVersionMatchError']">
                          <xsl:sort select="Item"/>
                          <tr>
                            <td width="100%" style="word-break:break-all" class="content">
                              <xsl:value-of select="Item"/>
                            </td>
                          </tr>
                        </xsl:for-each>
                      </table>
                    </td>
                  </tr>
                </table>
                <br/>
              </xsl:if>

              <xsl:if test="count(Issues/Issue[@IssueID='TipVersionMatchError'])>0">
                <table width="100%">
                  <tr>
                    <td width="15"></td>
                    <td>
                      <span style="margin-left:0px;font-size:12px;width:'100%'" class="overview">
                        <b>Failures from verification of latest tip versions</b>
                      </span>
                      <table cellspacing="1" cellpadding="1" class="infotable">
                        <tr>
                          <td width="100%" class="header">File</td>
                        </tr>
                        <xsl:for-each select="Issues/Issue[@IssueID='TipVersionMatchError']">
                          <xsl:sort select="Item"/>
                          <tr>
                            <td width="100%" style="word-break:break-all" class="content">
                              <xsl:value-of select="Item"/>
                            </td>
                          </tr>
                        </xsl:for-each>
                      </table>
                    </td>
                  </tr>
                </table>
                <br/>
              </xsl:if>

              <xsl:if test="count(Issues/Issue[@Type='Error' and @IssueID!='PinVersionMatchError' and @IssueID!='TipVersionMatchError'])>0">
                <table width="100%">
                  <tr>
                    <td width="15"></td>
                    <td>
                      <xsl:if test="$ReportName = 'VSSConverter' and Type='PostMigration'">
                        <span style="margin-left:0px;font-size:12px;width:'100%'" class="overview">
                          <b>Errors</b>
                        </span>
                      </xsl:if>
                      <table cellspacing="1" cellpadding="1" class="infotable">
                        <tr>
                          <td width="50%" class="header">
                            File or Folder
                          </td>
                          <td width="60%" class="header">Description</td>
                        </tr>
                        <xsl:for-each select="Issues/Issue[@Type='Error' and @IssueID!='PinVersionMatchError' and @IssueID!='TipVersionMatchError']">
                          <xsl:sort select="Item"/>
                          <tr>
                            <td width="50%" style="word-break:break-all" class="content">
                              <xsl:value-of select="Item"/>
                            </td>
                            <td width="50%" style="word-break:break-all" class="content">
                              <a href="http://go.microsoft.com/fwlink/?linkid=55082" style="color: blue;">
                                <xsl:value-of select="Message"/>
                              </a>
                            </td>
                          </tr>
                        </xsl:for-each>
                      </table>
                    </td>
                  </tr>
                </table>
                <br/>
              </xsl:if>
            </xsl:if>

            <xsl:if test="$CurConReport">
              <table width="100%">
                <tr>
                  <td width="15"></td>
                  <td>
                    <table cellspacing="1" cellpadding="1" class="infotable">
                      <tr>
                        <td width="20%" class="header">
                          Work Item Id
                        </td>
                        <td width="80%" class="header">Description</td>
                      </tr>
                      <xsl:for-each select="Issues/Issue[@Type='Error']">
                        <xsl:sort select="Item"/>
                        <tr>
                          <td width="20%" style="word-break:break-all" class="content">
                            <xsl:value-of select="Item"/>
                          </td>
                          <td width="80%" style="word-break:break-all" class="content">
                            <xsl:value-of select="Message"/>
                          </td>
                        </tr>
                      </xsl:for-each>
                    </table>
                  </td>
                </tr>
              </table>
            </xsl:if>
          </div>
        </xsl:if>


        <!-- Warnings Section for VSSConverter and SDConverter -->
        <xsl:if test="count(Issues/Issue[@Type='Warning'])>0 and $HatConReport">
          <a name="WarningSection" id="WarningSection">
            <h2>
              <IMG name="imgWarnings"  alt="expand/collapse section" align="center" class="expandable" onclick="changepic(this)"  src="_MigrationReport_Files/UpgradeReport_Minus.gif" width="21" child="srcWarnings"></IMG>
              Warnings
            </h2>
          </a>
          <div class="expanded" id="srcWarnings">
            <xsl:if test="count(Issues/Issue[@Type='Warning' and @IssueID!='FileCheckedOut' and @IssueID!='LabelNameChange' and @IssueID!='DependentMoveIn' and @IssueID!='DependentMoveOut' and @IssueID!= 'BranchIntoDependency' and @IssueID!= 'BranchFromDependency' and @IssueID!= 'OlderVersionNotStored' and @IssueID!= 'ChangeListDependency' and @IssueID!= 'VersionPurged' and @IssueID!= 'Share/Branch'])>0">
              <table width="100%">
                <tr>
                  <td width="15"></td>
                  <td>
                    <xsl:if test="$ReportName = 'SDConverter'">
                      <span style="margin-left:0px;font-size:12px;width:'100%'" class="overview">
                        <b>General Warnings</b>
                      </span>
                    </xsl:if>
                    <xsl:if test="$ReportName = 'VSSConverter'">
                      <span style="margin-left:0px;font-size:12px;width:'100%'" class="overview">
                        <b>Versions or history of VSS Files and Folders not retrieved by Converter</b>
                      </span>

                      <span style="margin-left:0px;font-size:11px;width:'100%'" class="overview">
                        <xsl:if test="$ReportType='PreMigration'">
                          The converter cannot scan the history of following files and folders, and these files and folders may not migrate properly. Click on the description of the warning for more details. This warning could be caused by  corruption in the  VSS database.
                        </xsl:if>
                        <xsl:if test="$ReportType='PostMigration'">
                          The converter cannot migrate following files and folders, or specific versions of these files and folders.
                        </xsl:if>
                        <a href="http://go.microsoft.com/fwlink/?linkid=55074" style="color: blue;">Click here for more information</a>
                      </span>
                    </xsl:if>

                    <table cellspacing="1" cellpadding="1" class="infotable">
                      <tr>
                        <td width="50%" class="header">File or Folder</td>
                        <td width="50%" class="header">Description</td>
                      </tr>
                      <xsl:for-each select="Issues/Issue[@Type='Warning' and @IssueID!='FileCheckedOut' and @IssueID!='LabelNameChange' and @IssueID!='DependentMoveIn' and @IssueID!='DependentMoveOut' and @IssueID!= 'BranchIntoDependency' and @IssueID!= 'BranchFromDependency' and @IssueID!= 'OlderVersionNotStored' and @IssueID!= 'ChangeListDependency' and @IssueID!= 'VersionPurged' and @IssueID!= 'Share/Branch']">
                        <xsl:sort select="Item"/>
                        <tr>
                          <td style="word-break:break-all" class="content">
                            <xsl:value-of select="Item"/>
                          </td>
                          <td style="word-break:break-all" class="content">
                            <xsl:value-of select="Message"/>
                          </td>
                        </tr>
                      </xsl:for-each>
                    </table>
                  </td>
                </tr>
              </table>
              <br/>
            </xsl:if>

            <!-- Folder Move Warnings for VSSConverter for FolderMoveIn -->
            <xsl:if test="count(Issues/Issue[@IssueID='DependentMoveIn'])>0 or count(Issues/Issue[@IssueID='DependentMoveOut'])>0 and $ReportName = 'VSSConverter'">
              <table width="100%">
                <tr>
                  <td width="15"></td>
                  <td>
                    <span style="margin-left:0px;font-size:12px;width:'100%'" class="overview">
                      <b>Data loss due to Folder Move</b>
                      <br/>
                    </span>
                    <xsl:if test="count(Issues/Issue[@IssueID='DependentMoveIn'])>0">
                      <span style="margin-left:0px;font-size:11px;width:'100%'" class="overview">
                        <xsl:if test="$ReportType='PreMigration'">
                          Only the destination folder is intended for migration, but one of its subfolders was moved from another (source) folder. After migration, the moved folder appears in the destination folder as though it was added when the folder was moved. The history of the moved folder and the items inside it prior to the move  is not migrated into Team Foundation Source Control. To prevent this type of data loss, migrate source and destination folders of moved VSS folders together. The following list shows these moved folders:
                        </xsl:if>
                        <xsl:if test="$ReportType='PostMigration'">
                          Only the destination folder was intended for migration, but one of its subfolder was moved from another (source) folder. After migration, the moved folder appears in the destination folder as though it was added when folder was moved. The history of the moved folder and the items inside it prior to move event is not migrated into Team Foundation Source Control. The following  list shows these moved folders:
                        </xsl:if>
                        <a href="http://go.microsoft.com/fwlink/?linkid=55078" style="color: blue;">Click here for more information</a>
                      </span>

                      <table cellspacing="1" cellpadding="1" class="infotable">
                        <tr>
                          <td width="30%" class="header">Moved VSS Folder</td>
                          <td width="35%" class="header">Source VSS Folder not under migration</td>
                          <td width="35%" class="header">Destination VSS folder under migration</td>
                        </tr>
                        <xsl:for-each select="Issues/Issue[@IssueID='DependentMoveIn']">
                          <xsl:sort select="Item"/>
                          <tr>
                            <td width="30%" style="word-break:break-all"  class="content">
                              <xsl:value-of select="Item"/>
                            </td>
                            <xsl:for-each select="AdditionalInfos/AdditionalInfo">
                              <td width="35%" style="word-break:break-all" class="content">
                                <xsl:value-of select="."/>
                              </td>
                            </xsl:for-each>
                          </tr>
                        </xsl:for-each>
                      </table>
                      <br/>
                    </xsl:if>

                    <!--- folder move section for VSSConverter FolderMoveOut -->
                    <xsl:if test="count(Issues/Issue[@IssueID='DependentMoveOut'])>0">
                      <span style="margin-left:0px;font-size:11px;width:'100%'" class="overview">
                        <xsl:if test="Type='PreMigration'">
                          Only the source folder is intended for migration, but one of its subfolders was moved to another (destination) folder. After migration, the history of the moved folder and the items inside it is not migrated into Team Foundation Source Control. To prevent this type of data loss, migrate source and destination folders of moved VSS folders together. The following list shows these moved folders:
                        </xsl:if>
                        <xsl:if test="Type='PostMigration'">
                          Only the source folder was intended for migration, but one of its subfolders was moved to another (destination) folder. The history of the moved folder and the items inside it is not migrated into Team Foundation Source Control. The following list shows these moved folders::
                        </xsl:if>
                        <a href="http://go.microsoft.com/fwlink/?linkid=55079" style="color: blue;">Click here for more information</a>
                      </span>

                      <table cellspacing="1" cellpadding="1" class="infotable">
                        <tr>
                          <td width="30%" class="header">Moved VSS Folder</td>
                          <td width="35%" class="header">Source VSS folder under migration</td>
                          <td width="35%" class="header">Destination VSS folder not under migration</td>
                        </tr>
                        <xsl:for-each select="Issues/Issue[@IssueID='DependentMoveOut']">
                          <xsl:sort select="Item"/>
                          <tr>
                            <td width="30%" style="word-break:break-all" class="content">
                              <xsl:value-of select="Item"/>
                            </td>
                            <xsl:for-each select="AdditionalInfos/AdditionalInfo">
                              <td width="35%" style="word-break:break-all" class="content">
                                <xsl:value-of select="."/>
                              </td>
                            </xsl:for-each>
                          </tr>
                        </xsl:for-each>
                      </table>
                    </xsl:if>

                  </td>
                </tr>
              </table>
              <br/>
            </xsl:if>

            <!-- Share/Branch Warnings Section for VSSConverter and SDConverter -->
            <xsl:if test="count(Issues/Issue[@IssueID='Share/Branch'])>0 and $ReportName = 'VSSConverter'">
              <table width="100%">
                <tr>
                  <td width="15"></td>
                  <td>
                    <span style="margin-left:0px;font-size:12px;width:'100%';" class="overview">
                      <b>Data loss due to Share/Branch</b>
                    </span>
                    <span style="margin-left:0px;font-size:11px;width:'100%';" class="overview">
                      <xsl:if test="Type='PreMigration'">
                        Sharing is not supported in Team Foundation Source Control. Shared files will be migrated by copying the version of the file at the time of sharing began to the destination folder. From then on, changes made to the shared file are replicated to both copies.

                        Sharing is a pre-condition of branching. The migration of a shared file will result in copying the file to the destination folder. Migration of the branch event means that the changes made to the shared file are not replicated to both copies, but instead  changes to each branch are migrated to the respective copy in Team Foundation Source  Control. Following folder contains one or more files shared or branched to other folders. See the history of the folders for more details about share and branch.
                      </xsl:if>
                      <xsl:if test="$ReportType='PostMigration'">
                        Sharing is not supported in Team Foundation Source Control. Shared files are migrated by copying the version of the file at the time sharing began to the destination folder. From then on, changes made to the shared file are replicated to both copies. 
                        
                        Sharing is a pre-condition of branching. The migration of a shared file results in copying the file to the destination folder. Migration of the branch event means that the changes made to the shared file are not replicated to both copies, but instead changes to each branch are migrated to the respective copy in Team Foundation Source Control. Following folder contains one or more files shared or branched to other folders. See the history of the folders for more details about share and branch.
                      </xsl:if>
                      <a href="http://go.microsoft.com/fwlink/?LinkId=55084" style="color: blue;">Click here for more information</a>
                    </span>
                    <table cellspacing="1" cellpadding="1" class="infotable">
                      <tr>
                        <td width="80%" class="header">
                          Folder
                        </td>
                        <td width="20%" class="header">
                          Share/Branch
                        </td>
                      </tr>
                      <xsl:for-each select="Issues/Issue[@IssueID='Share/Branch']">
                        <xsl:sort select="Item"/>
                        <tr>
                          <td width="80%" style="word-break:break-all" class="content">
                            <xsl:value-of select="Item"/>
                          </td>
                          <td width="20%" style="word-break:break-all"  class="content">
                            <xsl:value-of select="Location"/>
                          </td>
                        </tr>
                      </xsl:for-each>
                    </table>
                  </td>
                </tr>
              </table>
              <br/>
            </xsl:if>

            <!-- Time Zone issue for VSSConverter -->
            <xsl:if test="count(Issues/Issue[@IssueID='TimeZoneIssue'])>0 and $ReportName = 'VSSConverter'">
              <table width="100%">
                <tr>
                  <td width="15"></td>
                  <td>
                    <span style="margin-left:0px;font-size:12px;width:'100%'" class="overview">
                      <b>Time Zone Issue</b>
                    </span>
                    <span style="margin-left:0px;font-size:11px;width:'100%'" class="overview">
                      <xsl:choose>
                        <xsl:when test="$ReportType='PreMigration'">
                          The converter has diagnosed a time zone issue with the VSS Database under migration. The migration of this database may result in changed or lost information.
                        </xsl:when>
                        <xsl:when test="$ReportType='PostMigration'">
                          The converter has diagnosed a time zone issue with the VSS Database under migration. The migration may have resulted in changed or lost information.
                        </xsl:when>
                      </xsl:choose>
                      <a href="http://go.microsoft.com/fwlink/?linkid=55080" style="color: blue;">Click here for more information</a>
                    </span>
                  </td>
                </tr>
              </table>
              <br/>
            </xsl:if>

            <!-- Files Checkout Warnings Section for VSSConverter and SDConverter -->
            <xsl:if test="count(Issues/Issue[@IssueID='FileCheckedOut'])>0">
              <table width="100%">
                <tr>
                  <td width="15"></td>
                  <td>
                    <span style="margin-left:0px;font-size:12px;width:'100%';" class="overview">
                      <b>Files Checkout Warnings</b>
                    </span>
                    <span style="margin-left:0px;font-size:11px;width:'100%';" class="overview">
                      <xsl:if test="Type='PreMigration'">
                        List of files currently checked out. The migration process does not preserve check-out information.
                      </xsl:if>
                      <xsl:if test="$ReportType='PostMigration'">
                        List of files that were checked out during the migration process. Check-out status information is not preserved.
                      </xsl:if>
                      <a href="http://go.microsoft.com/fwlink/?linkid=55076" style="color: blue;">Click here for more information</a>
                    </span>
                    <table cellspacing="1" cellpadding="1" class="infotable">
                      <tr>
                        <td width="80%" class="header">
                          <xsl:if test="$ReportName = 'VSSConverter'">
                            VSS Filename
                          </xsl:if>
                          <xsl:if test="$ReportName = 'SDConverter'">
                            Source Depot Filename
                          </xsl:if>
                        </td>
                        <td width="20%" class="header">
                          <xsl:if test="$ReportName = 'VSSConverter'">
                            VSS Username
                          </xsl:if>
                          <xsl:if test="$ReportName = 'SDConverter'">
                            Source Depot Username
                          </xsl:if>
                        </td>
                      </tr>
                      <xsl:for-each select="Issues/Issue[@IssueID='FileCheckedOut']">
                        <xsl:sort select="Item"/>
                        <tr>
                          <td width="80%" style="word-break:break-all" class="content">
                            <xsl:value-of select="Item"/>
                          </td>
                          <td width="20%" style="word-break:break-all"  class="content">
                            <xsl:value-of select="Location"/>
                          </td>
                        </tr>
                      </xsl:for-each>
                    </table>
                  </td>
                </tr>
              </table>
              <br/>
            </xsl:if>

            <!-- Changelists spanning across folders under migration and not under migration Warnings Section for SDConverter -->
            <xsl:if test="count(Issues/Issue[@IssueID='ChangeListDependency'])>0 and $ReportName = 'SDConverter'">
              <table width="100%">
                <tr>
                  <td width="15"></td>
                  <td>
                    <span style="margin-left:0px;font-size:12px;width:'100%'" class="overview">
                      <b>Changelists spanning across folders under migration and folders not under migration</b>
                    </span>
                    <span style="margin-left:0px;font-size:11px;width:'100%'" class="overview">
                      <xsl:if test="$ReportType='PreMigration'">
                        Analysis found Source Depot changelists with actions in folders under migration and not under migration. During migration, only the actions corresponding to the folders under migration will migrate.
                      </xsl:if>
                      <xsl:if test="$ReportType='PostMigration'">
                        Migration found Source Depot changelists with actions in folders under migration and not under migration. During migration, only the actions corresponding to the folders under migration are migrated.
                      </xsl:if>
                    </span>
                    <table cellspacing="1" cellpadding="1" class="infotable">
                      <tr>
                        <td width="50%" class="header">Source Depot Folder not under migration</td>
                        <td width="50%" class="header">Changelist</td>
                      </tr>
                      <xsl:for-each select="Issues/Issue[@IssueID='ChangeListDependency']">
                        <xsl:sort select="Item"/>
                        <tr>
                          <td width="50%" style="word-break:break-all" class="content">
                            <xsl:value-of select="Item"/>
                          </td>
                          <td width="50%" style="word-break:break-all" class="content">
                            <xsl:value-of select="Location"/>
                          </td>
                        </tr>
                      </xsl:for-each>
                    </table>
                  </td>
                </tr>
              </table>
              <br/>
            </xsl:if>

            <!-- Label Name Change section for VSSConverter and SDConverter -->
            <xsl:if test="count(Issues/Issue[@IssueID='LabelNameChange'])>0">
              <table width="100%">
                <tr>
                  <td width="15"></td>
                  <td>
                    <span style="margin-left:0px;font-size:12px;width:'100%'" class="overview">
                      <b>Label Name Change Warnings</b>
                    </span>
                    <span style="margin-left:0px;font-size:11px;width:'100%'" class="overview">
                      <xsl:if test="Type='PreMigration'">
                        <xsl:choose>
                          <xsl:when test ="$ReportName='VSSConverter'">
                            The following list contains the label names that will change as a result of migration because they contain characters not supported by Team Foundation Source Control as label name characters:
                          </xsl:when>
                          <xsl:when test ="$ReportName='SDConverter'">
                            Label names will change as a result of migration because they contain characters not supported by Team Foundation source control, or they contain more than 64 characters. Team Foundation source control supports a maximum label size of 64 characters.
                          </xsl:when>
                        </xsl:choose>
                      </xsl:if>
                      <xsl:if test="Type='PostMigration'">
                        <xsl:choose>
                          <xsl:when test ="$ReportName='VSSConverter'">
                            The following list contains the label names that changed as a result of migration because they contained characters not supported by Team Foundation Source Control as label name characters:
                          </xsl:when>
                          <xsl:when test ="$ReportName='SDConverter'">
                            Label names changed as a result of migration because they contained characters not supported by Team Foundation, or they contained more that 64 characters. Team Foundation source control supports a maximum label size of 64 characters.
                          </xsl:when>
                        </xsl:choose>
                      </xsl:if>
                      <a href="http://go.microsoft.com/fwlink/?linkid=55075" style="color: blue;">Click here for more information</a>
                    </span>
                    <table  cellspacing="1" cellpadding="1" class="infotable">
                      <tr>
                        <td width="50%" class="header">
                          <xsl:choose>
                            <xsl:when test="$ReportName='VSSConverter'">
                              VSS Label
                            </xsl:when>
                            <xsl:when test="$ReportName='SDConverter'">
                              Source Depot Label
                            </xsl:when>
                          </xsl:choose>
                        </td>
                        <td width="50%" class="header">Team Foundation Label</td>
                      </tr>
                      <xsl:for-each select="Issues/Issue[@IssueID='LabelNameChange']">
                        <xsl:sort select="Item"/>
                        <tr>
                          <xsl:for-each select="AdditionalInfos/AdditionalInfo">
                            <td width="50%" style="word-break:break-all" class="content">
                              <xsl:value-of select="."/>
                            </td>
                          </xsl:for-each>
                        </tr>
                      </xsl:for-each>
                    </table>
                  </td>
                </tr>
              </table>
              <br/>
            </xsl:if>

            <!-- Files with only latest version stored for VSSConverter -->
            <xsl:if test="count(Issues/Issue[@IssueID='OlderVersionNotStored'])>0 and $ReportName = 'VSSConverter'">
              <table width="100%">
                <tr>
                  <td width="15"></td>
                  <td>
                    <span style="margin-left:0px;font-size:12px;width:'100%'" class="overview">
                      <b>Files with VSS feature to keep latest version only</b>
                    </span>
                    <span style="margin-left:0px;font-size:11px;width:'100%'" class="overview">
                      The following is the list of files with that have the VSS property ‘Keep latest version only’ enabled. Since previous versions of these files are not retrievable, for each version of file, converter creates an empty file with no content.
                      <a href="http://go.microsoft.com/fwlink/?linkid=55077" style="color: blue;">Click here for more information</a>
                    </span>
                    <table cellspacing="1" cellpadding="1" class="infotable">
                      <tr>
                        <td class="header">Only latest file version is stored.</td>
                      </tr>
                      <xsl:for-each select="Issues/Issue[@IssueID='OlderVersionNotStored']">
                        <xsl:sort select="Item"/>
                        <tr>
                          <td style="word-break:break-all" class="content">
                            <xsl:value-of select="Item"/>
                          </td>
                        </tr>
                      </xsl:for-each>
                    </table>
                  </td>
                </tr>
              </table>
              <br/>
            </xsl:if>

            <!-- Loss of Branch/Merge Information SDConverter for BranchFromDependency & BranchIntoDependency  -->
            <xsl:if test="count(Issues/Issue[@IssueID='BranchFromDependency'])>0  or count(Issues/Issue[@IssueID='BranchIntoDependency'])>0 and $ReportName = 'SDConverter'">
              <table width="100%">
                <tr>
                  <td width="15"></td>
                  <td>
                    <span style="margin-left:0px;font-size:12px;width:'100%'" class="overview">
                      <b>Loss of Branch/Merge Information</b>
                    </span>

                    <!-- Loss of Branch/Merge Information SDConverter for BranchFromDependency  -->
                    <xsl:if test="count(Issues/Issue[@IssueID='BranchFromDependency'])>0">
                      <span style="margin-left:0px;font-size:11px;width:'100%'" class="overview">
                        <xsl:if test="Type='PreMigration'">
                          Analysis found files in a folder being migrated that are branched from other folders that are not being migrated. During migration, the branch and merge information will be lost. The branch action appears as an add. To prevent the loss of information, migrate these folders together.
                        </xsl:if>
                        <xsl:if test="Type='PostMigration'">
                          Migration found files in the folder being migrated that were branched from other folders that were not being migrated. During migration, the branch and merge information is lost.
                        </xsl:if>
                      </span>
                      <table cellspacing="1" cellpadding="1" class="infotable">
                        <tr>
                          <td width="50%" class="header">Source Depot Folder not under migration</td>
                          <td width="50%" class="header">Changelist</td>
                        </tr>
                        <xsl:for-each select="Issues/Issue[@IssueID='BranchFromDependency']">
                          <xsl:sort select="Item"/>
                          <tr>
                            <td width="50%" style="word-break:break-all" class="content">
                              <xsl:value-of select="Item"/>
                            </td>
                            <td width="50%" style="word-break:break-all" class="content">
                              <xsl:value-of select="Location"/>
                            </td>
                          </tr>
                        </xsl:for-each>
                      </table>
                      <br/>
                    </xsl:if>

                    <!-- Loss of Branch/Merge Information SDConverter for BranchIntoDependency  -->
                    <xsl:if test="count(Issues/Issue[@IssueID='BranchIntoDependency'])>0">
                      <span style="margin-left:0px;font-size:11px;width:'100%'" class="overview">
                        <xsl:if test="$ReportType='PreMigration'">
                          Analysis found files in a folder being migrated that are branched to other folders that are not being migrated. During migration, the branch and merge information will be lost. To prevent the loss of information, migrate these folders together.
                        </xsl:if>
                        <xsl:if test="$ReportType='PostMigration'">
                          Migration found files in the folder being migrated that are branched to other folders that are not being migrated. During migration, the branch and merge information is lost.
                        </xsl:if>
                      </span>
                      <table cellspacing="1" cellpadding="1" class="infotable">
                        <tr>
                          <td width="50%" class="header">Source Depot Folder not under migration</td>
                          <td width="50%" class="header">Changelist</td>
                        </tr>
                        <xsl:for-each select="Issues/Issue[@IssueID='BranchIntoDependency']">
                          <xsl:sort select="Item"/>
                          <tr>
                            <td width="50%" style="word-break:break-all" class="content">
                              <xsl:value-of select="Item"/>
                            </td>
                            <td width="50%" style="word-break:break-all" class="content">
                              <xsl:value-of select="Location"/>
                            </td>
                          </tr>
                        </xsl:for-each>
                      </table>
                    </xsl:if>
                  </td>
                </tr>
              </table>
              <br/>
            </xsl:if>

            <!-- Files Files with previous version purged Warnings Section for SDConverter -->
            <xsl:if test="count(Issues/Issue[@IssueID='VersionPurged'])>0 and $ReportName = 'SDConverter'">
              <table width="100%">
                <tr>
                  <td width="15"></td>
                  <td>
                    <span style="margin-left:0px;font-size:12px;width:'100%'" class="overview">
                      <b>Files with previous version purged</b>
                    </span>
                    <span style="margin-left:0px;font-size:11px;width:'100%'" class="overview">
                      Source Depot allows a user to remove previous versions of files, so the previous versions of some files do not exist. The latest versions will be migrated, but for all previous versions of these files, the converter creates an empty file in Team Foundation.
                    </span>
                    <table cellspacing="1" cellpadding="1" class="infotable">
                      <tr>
                        <td width="50%" class="header">Source Depot Filename</td>
                        <td width="50%" class="header">Purged version</td>
                      </tr>
                      <xsl:for-each select="Issues/Issue[@IssueID='VersionPurged']">
                        <xsl:sort select="Item"/>
                        <tr>
                          <td width="50%" style="word-break:break-all" class="content">
                            <xsl:value-of select="Item"/>
                          </td>
                          <td width="50%" style="word-break:break-all" class="content">
                            <xsl:value-of select="Location"/>
                          </td>
                        </tr>
                      </xsl:for-each>
                    </table>
                  </td>
                </tr>
              </table>
              <br/>
            </xsl:if>

          </div>
        </xsl:if>


        <!-- Warnings Section for CQConverter and PSConverter -->
        <xsl:if test="count(Issues/Issue[@Type='Warning'])>0 and $CurConReport">
          <a name="WarningSection"  id="WarningSection">
            <h2>
              <IMG name="imgWarnings" alt="expand/collapse section" align="center" class="expandable" onclick="changepic(this)" src="_MigrationReport_Files/UpgradeReport_Minus.gif" width="21" child="srcWarnings"></IMG>
              Warnings
            </h2>
          </a>
          <div class="expanded" id="srcWarnings">
            <table width="100%">
              <!-- Work Item Type Definition Related Warnings -->
              <xsl:if test="count(Issues/Issue[@Type='Warning' and @Group='Witd'])>0 and $CurConReport">
                <tr>
                  <td width="15"></td>
                  <td>
                    <span style="margin-left:0px;font-size:12px;width:'100%'" class="overview">
                      <b>Work item type definition related warnings</b>
                    </span>
                    <table cellspacing="1" cellpadding="1" class="infotable">
                      <tr>
                        <td width="20%" class="header">Entity</td>
                        <td width="80%" class="header">Warning</td>
                      </tr>
                      <xsl:for-each select="Issues/Issue[@Type='Warning' and @Group='Witd']">
                        <tr>
                          <td class="content">
                            <xsl:value-of select="Item"/>
                          </td>
                          <td class="content">
                            <xsl:value-of select="Message"/>
                          </td>
                        </tr>
                      </xsl:for-each>
                    </table>
                  </td>
                </tr>
              </xsl:if>
              <!-- End Work Item Type Definition Related Warnning -->

              <!-- Work Item Migration Warnings -->
              <xsl:if test="count(Issues/Issue[@Type='Warning' and @Group='Wi'])>0 and $CurConReport">
                <tr>
                  <td width="15"></td>
                  <td>
                    <span style="margin-left:0px;font-size:12px;width:'100%'" class="overview">
                      <b>Work item migration related warnings</b>
                    </span>
                    <table cellspacing="1" cellpadding="1" class="infotable">
                      <tr>
                        <td width="20%" class="header">Source Work Item ID</td>
                        <td width="80%" class="header">Warning</td>
                      </tr>
                      <xsl:for-each select="Issues/Issue[@Type='Warning' and @Group='Wi']">
                        <tr>
                          <td class="content">
                            <xsl:value-of select="Item"/>
                          </td>
                          <td class="content">
                            <xsl:value-of select="Message"/>
                          </td>
                        </tr>
                      </xsl:for-each>
                    </table>
                  </td>
                </tr>
              </xsl:if>
              <!-- End Work Item Migration Warnings -->

              <!-- General/Other Warnings -->
              <xsl:if test="count(Issues/Issue[@Type='Warning' and @Group!='Wi' and @Group!='Witd'])>0 and $CurConReport">
                <tr width="100%">
                  <td width="15"></td>
                  <td>
                    <span style="margin-left:0px;font-size:12px;width:'100%'" class="overview">
                      <b>General Warnings</b>
                    </span>
                    <table cellspacing="1" cellpadding="1" class="infotable">
                      <tr>
                        <td class="header">Warning</td>
                      </tr>
                      <xsl:for-each select="Issues/Issue[@Type='Warning' and @Group!='Wi' and @Group!='Witd']">
                        <tr>
                          <td class="content">
                            <xsl:value-of select="Message"/>
                          </td>
                        </tr>
                      </xsl:for-each>
                    </table>
                  </td>
                </tr>
              </xsl:if>
              <!-- End General/Other Warnings -->
            </table>
            <br/>
          </div>
        </xsl:if>

        <!-- Output Section -->
        <xsl:if test="count(output)>0">
          <h2>
            <IMG alt="expand/collapse section" align="center" class="expandable" onclick="changepic(this)" src="_MigrationReport_Files/UpgradeReport_Minus.gif" width="21" child="srcOutput"></IMG>
            Output
          </h2>
          <div class="expanded" id="srcOutput">
            <table cellspacing="1" cellpadding="1" class="infotable">
              <tr>
                <td width="15"></td>
                <td class="header">File Name</td>
                <td class="header">File Location</td>
              </tr>
              <xsl:for-each select="Output/File">
                <tr>
                  <td class="content">
                    <xsl:value-of select="@Name"/>
                  </td>
                  <td class="content">
                    <xsl:value-of select="."/>
                  </td>
                </tr>
              </xsl:for-each>
            </table>
          </div>
        </xsl:if>

        <!-- Analyze Output Section -->
        <xsl:if test="count(Output)>0 and Type='PreMigration' and $CurConReport">
          <h2>
            <IMG alt="expand/collapse section" align="center" class="expandable" onclick="changepic(this)" src="_MigrationReport_Files/UpgradeReport_Minus.gif" width="21" child="srcAnalyzeOutput"></IMG>
            Output
          </h2>
          <div class="expanded" id="srcAnalyzeOutput">
            <table width="100%">
              <tr>
                <td width="15"></td>
                <td>
                  <table cellspacing="1" cellpadding="1" class="infotable">
                    <tr>
                      <td width="15%" class="header">File Type</td>
                      <td class="header">File Name</td>
                    </tr>
                    <xsl:for-each select="Output/File">
                      <tr>
                        <td class="content">
                          <xsl:value-of select="@Name"/>
                        </td>
                        <td class="content">
                          <xsl:value-of select="."/>
                        </td>
                      </tr>
                    </xsl:for-each>
                  </table>
                </td>
              </tr>
            </table>
          </div>
        </xsl:if>


        <!-- User Input Section -->
        <h2>
          <IMG alt="expand/collapse section" align="center" class="expandable" onclick="changepic(this)" src="_MigrationReport_Files/UpgradeReport_Minus.gif" width="21" child="srcUserInput"></IMG>
          User Input
        </h2>
        <div class="expanded" id="srcUserInput">
          <table width="100%">
            <tr>
              <td width="15"></td>
              <td>
                <table cellspacing="1" cellpadding="1" class="infotable">
                  <tr>
                    <td width="15%" class="header">Command-line</td>
                    <td style="word-break:break-all" class="content">
                      <xsl:value-of select="UserInput/CommandLine"/>
                    </td>
                  </tr>
                  <xsl:if test="count(UserInput/Options/*)>0">
                    <tr>
                      <td class="header" valign="top">
                        <xsl:attribute name="rowspan">
                          <xsl:value-of select="count(UserInput/Options/*)"/>
                        </xsl:attribute>
                        Command-Line Options
                      </td>
                      <td class="content">
                        <xsl:value-of select="UserInput/Options/Option[1]"/>
                      </td>
                    </tr>
                    <xsl:for-each select="UserInput/Options/Option">
                      <xsl:if test="position()!=1">
                        <tr>
                          <td class="content">
                            <xsl:value-of select="."/>
                          </td>
                        </tr>
                      </xsl:if>
                    </xsl:for-each>
                  </xsl:if>
                </table>
              </td>
            </tr>
          </table>
        </div>
      </body>
    </html>
  </xsl:template>
</xsl:stylesheet>