if "%1"=="/?" goto USAGE
    if "%1"=="" goto USAGE
     
    cd "C:\Program Files (x86)\Microsoft SQL Server\110\Tools\Binn"
    osql -E -i C:\Users\adriscol\Source\Repos\TfsIntegrationPlatform\IntegrationPlatform\Setup\Installation\Database\Tfs_IntegrationPlatform.sql -S %1 -n
     
    goto END
     
    :USAGE
   echo dropAndDeployDB.cmd <servername>
    
   :END