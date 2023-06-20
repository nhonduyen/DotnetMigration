.Net core demo migrate data change from database A to database B
The solution contains 2 project:
- A web api of source database
- A service worker will run 10s per time to migrate data change to database B

Tech:
- Timestamp
- Temp table
- Bulk copy
- Bulk merge
- Bulk update