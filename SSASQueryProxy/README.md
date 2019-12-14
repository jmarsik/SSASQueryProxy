# SSAS query proxy

## Testing from command line

```
.\curl.exe --insecure --user "USER:PASSWORD" --data-ascii "MDXQUERY" "https://localhost:44380/api/query?server=SERVERorHTTPMSMDPUMPURL&db=DATABASE"
```
