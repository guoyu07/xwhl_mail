using System;
using System.Text.RegularExpressions;

namespace Szac.Odr.Utils
{
    public class PetaPocoMapper : PetaPoco.IMapper
    {
        public void GetTableInfo(Type t, PetaPoco.TableInfo ti)
        {
            if (t.GetCustomAttributes(typeof(PetaPoco.TableNameAttribute), true).Length == 0)
            {
                ti.TableName = ToDBName(ti.TableName);
            }
        }

        public bool MapPropertyToColumn(System.Reflection.PropertyInfo pi, ref string columnName, ref bool resultColumn)
        {
            if (pi.GetCustomAttributes(typeof(PetaPoco.ColumnAttribute), true).Length == 0)
            {
                columnName = ToDBName(columnName);
                return true;
            }
            return false;
        }

        public Func<object, object> GetFromDbConverter(System.Reflection.PropertyInfo pi, Type SourceType)
        {
            return null;
        }

        public Func<object, object> GetFromDbConverter(Type DestType, Type SourceType)
        {
            return null;
        }

        public Func<object, object> GetToDbConverter(Type SourceType)
        {
            return null;
        }

        private string ToDBName(string codeName)
        {
            // codeName is PascalCase or camelCase, eg: FirstName, lastName
            // DBName is splitted by undercores, eg: first_name, last_name

            return Regex.Replace(codeName, "([a-z](?=[A-Z])|[A-Z](?=[A-Z][a-z]))", "$1_").ToLower();
        }
    }
}
