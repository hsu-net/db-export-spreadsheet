# Hsu.Db.Export.Spreadsheet

[![dev](https://github.com/hsu-net/db-export-spreadsheet/actions/workflows/build.yml/badge.svg?branch=dev)](https://github.com/hsu-net/db-export-spreadsheet/actions/workflows/build.yml)
[![preview](https://github.com/hsu-net/db-export-spreadsheet/actions/workflows/deploy.yml/badge.svg?branch=preview)](https://github.com/hsu-net/db-export-spreadsheet/actions/workflows/deploy.yml)
[![main](https://github.com/hsu-net/db-export-spreadsheet/actions/workflows/deploy.yml/badge.svg?branch=main)](https://github.com/hsu-net/db-export-spreadsheet/actions/workflows/deploy.yml)
[![Nuke Build](https://img.shields.io/badge/Nuke-Build-yellow.svg)](https://github.com/nuke-build/nuke)
[![FreeSql](https://img.shields.io/nuget/v/FreeSql?style=flat-square&label=FreeSql)](https://www.nuget.org/packages/FreeSql)
[![OpenXml](https://img.shields.io/nuget/v/DocumentFormat.OpenXml?style=flat-square&label=OpenXml)](https://www.nuget.org/packages/DocumentFormat.OpenXml)


A component that regularly exports database tables to spreadsheets every day, can export Excel and Csv files, and supports Excel templates.

## Package Version

| Name | Source | Stable | Preview |
|---|---|---|---|
| Hsu.Db.Export.Spreadsheet | Nuget | [![NuGet](https://img.shields.io/nuget/v/Hsu.Db.Export.Spreadsheet?style=flat-square)](https://www.nuget.org/packages/Hsu.Db.Export.Spreadsheet) | [![NuGet](https://img.shields.io/nuget/vpre/Hsu.Db.Export.Spreadsheet?style=flat-square)](https://www.nuget.org/packages/Hsu.Db.Export.Spreadsheet) |
| Hsu.Db.Export.Spreadsheet | MyGet | [![MyGet](https://img.shields.io/myget/godsharp/v/Hsu.Db.Export.Spreadsheet?style=flat-square&label=myget)](https://www.myget.org/feed/godsharp/package/nuget/Hsu.Db.Export.Spreadsheet) | [![MyGet](https://img.shields.io/myget/godsharp/vpre/Hsu.Db.Export.Spreadsheet?style=flat-square&label=myget)](https://www.myget.org/feed/godsharp/package/nuget/Hsu.Db.Export.Spreadsheet) |

## Getting Started

### Install Packages

  - Hsu.Db.Export.Spreadsheet
  - FreeSql

### Configure DependencyInjection

  ```csharp
  static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
  {
      services.AddDailySyncSpreadsheet();
      ConfigureFreeSql(services, configuration);
  }
  
  static void ConfigureFreeSql(IServiceCollection services, IConfiguration configuration)
  {
      var freeSql = new FreeSqlBuilder()
          .UseConnectionString(DataType.MySql, configuration.GetConnectionString("Default"))
          .Build();
  
      freeSql.Aop.CommandAfter += (_, e) =>
      {
          if (e.Exception != null)
          {
              Log.Logger.Error("Message:{Message}\r\nStackTrace:{StackTrace}", e.Exception.Message, e.Exception.StackTrace);
          }
  
          Log.Logger.Debug("FreeSql>A>{Sql}", e.Command.CommandText);
      };
      
      freeSql.Aop.CommandBefore += (_, e) =>
      {
          Log.Logger.Debug("FreeSql>B>{Sql}", e.Command.CommandText);
      };
  
      services.AddSingleton(freeSql);
  }
  ```

### Add Export Options

- Update ConnectionString
- Update Export Options

> `Template` only support `Excel` export

```json
{
  "ConnectionStrings": {
    "Default": "Data Source=mysql.sqlpub.com;Port=3306;User ID=public;Initial Catalog=db_hsu_des;Charset=utf8;SslMode=none;Min pool size=1"
  },
  "Export": {
    "Spreadsheet": {
      "Trigger": "00:00:00",
      "Launch": true,
      "Interval": "00:00:30",
      "Timeout": "00:01:30",
      "Path": null,
      "Tables": [
        {
          "Name": "Employees",
          "Code": "employees",
          "Filter": "create_at",
          "Chunk": 5000,
          "AscOrder": true,
          "Output": "Csv",
          "Fields": [
            {
              "Name": "EmployeeNo",
              "Column": "emp_no",
              "Type": "Int32"
            },
            {
              "Name": "Birthdate",
              "Column": "birth_date",
              "Type": "DateTime",
              "Format": "yyyy-MM-dd"
            },
            {
              "Name": "First Name",
              "Column": "first_name",
              "Type": "String"
            },
            {
              "Name": "Last Name",
              "Column": "last_name",
              "Type": "String"
            },
            {
              "Name": "Gender",
              "Column": "gender",
              "Type": "String",
              "Format": "yyyy-MM-dd"
            },
            {
              "Name": "Hire Date",
              "Column": "hire_date",
              "Type": "DateTime",
              "Format": "yyyy-MM-dd"
            },
            {
              "Name": "Create At",
              "Column": "create_at",
              "Type": "DateTime"
            }
          ]
        },
        {
          "Name": "Titles",
          "Code": "titles",
          "Filter": "create_at",
          "Chunk": 5000,
          "AscOrder": true,
          "Output": "Xlsx",
          "Fields": [
            {
              "Name": "EmployeeNo",
              "Column": "emp_no",
              "Type": "Int32"
            },
            {
              "Name": "Title",
              "Column": "title",
              "Type": "String"
            },
            {
              "Name": "From Date",
              "Column": "from_date",
              "Type": "DateTime",
              "Format": "yyyy-MM-dd"
            },
            {
              "Name": "To Date",
              "Column": "to_date",
              "Type": "DateTime",
              "Format": "yyyy-MM-dd"
            },
            {
              "Name": "Create At",
              "Column": "create_at",
              "Type": "DateTime"
            }
          ]
        },
        {
          "Name": "Salaries",
          "Code": "salaries",
          "Filter": "create_at",
          "Chunk": 5000,
          "AscOrder": false,
          "Template": "ExportTemplate.xlsx",
          "Output": "Xlsx",
          "Fields": [
            {
              "Name": "EmployeeNo",
              "Column": "emp_no",
              "Property": "EmployeeNo",
              "Type": "Int32"
            },
            {
              "Name": "Salary",
              "Column": "salary",
              "Property": "Salary",
              "Type": "Int32",
              "Format": "0000.00"
            },
            {
              "Name": "From Date",
              "Column": "from_date",
              "Property": "FromDate",
              "Type": "DateTime",
              "Format": "yyyy-MM-dd"
            },
            {
              "Name": "To Date",
              "Column": "to_date",
              "Property": "ToDate",
              "Type": "DateTime",
              "Format": "yyyy-MM-dd"
            },
            {
              "Name": "Create At",
              "Column": "create_at",
              "Type": "DateTime"
            }
          ]
        }
      ]
    }
  }
}
```
- Table
> - Trigger : The time to sync tables to local.
> - Launch : if true will execute once at startup
> - Timeout : The time to wait for table read operations
> - Path : The path that export file storage
> - Output : Only `Csv` and `Xlsx`
> - Chunk : The size of the chunk per read from the database

- Fields
> - Property : The name of the property for object to export,if null use `Column`
> - Type : The type of field, default is `String`
> - Nullable : The field is nullable
> - Template : The template excel file,only `Xlsx` output
> - Format : The format for `IFormattable`
> - Escape : if true escape the value, default is false

## Template Format

The template format is `Field`.

## Fields Type Format

- Boolean
- Byte
- Char
- Int16
- Int32
- Int64
- SByte
- UInt16
- UInt32
- UInt64
- Single
- Double
- Decimal
- String
- DateTime
- Enum

## License

  MIT