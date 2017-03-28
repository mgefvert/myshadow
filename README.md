# myshadow

Developer tool to copy production databases to local developer databases. (It makes a local "shadow" of the production data.) 

Depends on mysql command line tools to run mysqldump / mysql with appropriate parameters to make local copies and optionally
apply transformations to the local data (if you're changing the schema for local development, for instance).

## Overview

Basically, it runs the mysqldump and mysql command-line tools to download a remote database, store it in a local file,
recreate the local database at will and optionally apply transformations. By crafting the shadow definition file in various ways
you can exclude large tables, manipulate the data and so on to make your daily development life easier. Most of what
it does is just craft a special command line and let mysql do the grunt work - but it takes some redundant work out of
your life :)

## Usage

Format: myshadow [-v] definition-file [commands...]

Available commands:
   
* dump - dump remote database to a local file
* reload - recreate local database from file
* local-schema - dump the local database schema
* remote-schema - dump the remote database schema
* transform - apply transformations to local file

If no commands are given, 'dump reload transform' will be assumed.

Available options:

* -a or --remove-auto - remove auto_increment parameter from the schema dump (requires 'sed' installed)
* -v or --verbose - display output from mysql

## Example shadow file

```
# Remote server definition
remote-server   = --login-path=root.remote
remote-database = customers

# Local server definition
local-server    = --login-path=root.local
local-database  = customers
local-collation = utf8mb4_general_ci

# Data file
data-file       = customers.sql

# Transform file can be used to automatically apply transformations to the local database
transform-file  = customers-transform.sql

# Extra parameters - anything that starts with a dash will simply be appended to the mysqldump command line
--ignore-table customers.log
--ignore-table customers.reports

# Separate table dump definitions, starts with "table = " and then uses extra parameters
table = log
--where "time >= date_sub(curdate(), interval 1 month)"

table = reports
--where "created >= date_sub(curdate(), interval 1 month)"
```

## Complaints

Open an issue if you have any complaints. I use this tool in my daily job so chances are I'll look at it.
