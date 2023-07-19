# Hsu.Db.Export.Spreadsheet

[![dev](https://github.com/hsu-net/db-export-spreadsheet/actions/workflows/build.yml/badge.svg?branch=dev)](https://github.com/hsu-net/db-export-spreadsheet/actions/workflows/build.yml)
[![preview](https://github.com/hsu-net/db-export-spreadsheet/actions/workflows/deploy.yml/badge.svg?branch=preview)](https://github.com/hsu-net/db-export-spreadsheet/actions/workflows/deploy.yml)
[![main](https://github.com/hsu-net/db-export-spreadsheet/actions/workflows/deploy.yml/badge.svg?branch=main)](https://github.com/hsu-net/db-export-spreadsheet/actions/workflows/deploy.yml)
[![Nuke Build](https://img.shields.io/badge/Nuke-Build-yellow.svg)](https://github.com/nuke-build/nuke)
[![FreeSql](https://img.shields.io/nuget/v/FreeSql?style=flat-square&label=FreeSql)](https://www.nuget.org/packages/FreeSql)
[![MiniExcel](https://img.shields.io/nuget/v/FreeSql?style=flat-square&label=MiniExcel)](https://www.nuget.org/packages/MiniExcel)

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
  
          Log.Logger.Debug("FreeSql>{Sql}", e.Command.CommandText);
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
        "Default": "Data Source=127.0.0.1;Port=3306;User ID=root;Password=root;Initial Catalog=mysql;Charset=utf8;SslMode=none;Min pool size=1"
    },
    "Export": {
        "Spreadsheet": {
            "Trigger": "00:00:00",
            "Path": null,
            "Tables": [
                {
                    "Name": "Csv测试表",
                    "Code": "TestCsv",
                    "Filter": "CreateTime",
                    "AscOrder": true,
                    "Output": "Csv",
                    "Fields": [
                        {
                            "Name": "标识",
                            "Column": "Id",
                            "Type": "Int64"
                        },
                        {
                            "Name": "字符串",
                            "Column": "String",
                            "Type": "String"
                        },
                        {
                            "Name": "整型",
                            "Column": "Integer",
                            "Type": "Int32"
                        },
                        {
                            "Name": "布尔",
                            "Column": "Bool",
                            "Type": "Boolean"
                        },
                        {
                            "Name": "创建时间",
                            "Column": "CreateTime",
                            "Type": "DateTime"
                        }
                    ]
                },
                {
                    "Name": "测试Xlsx",
                    "Code": "TestXlsx",
                    "Filter": "CreateTime",
                    "AscOrder": false,
                    "Template": "template.xlsx",
                    "Output": "Xlsx",
                    "Fields": [
                        {
                            "Name": "标识",
                            "Column": "Id",
                            "Type": "Int64"
                        },
                        {
                            "Name": "字符串",
                            "Column": "String",
                            "Type": "String"
                        },
                        {
                            "Name": "整型",
                            "Column": "Integer",
                            "Type": "Int32"
                        },
                        {
                            "Name": "布尔",
                            "Column": "Bool",
                            "Type": "Boolean"
                        },
                        {
                            "Name": "创建时间",
                            "Column": "CreateTime",
                            "Type": "DateTime"
                        }
                    ]
                }
            ]
        }
    }
}
```

> - Trigger : The time to sync tables to local.
> - Path : The path that export file storage
> - Output : Only `Csv` and `Xlsx`
> - Template : The template excel file,only `Xlsx` output.

## Template Format

The template format is `{{_.Field}}`.

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

## License

  MIT