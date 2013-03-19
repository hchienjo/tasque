//
// Database.cs
//
// Original header:
// Database.cs created with MonoDevelop
// User: calvin at 11:27 AM 2/19/2008
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//
// Author:
//       Unknown author
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using Mono.Data.Sqlite;
using System.IO;
using Tasque;

namespace Tasque.Data.Sqlite
{
	public class Database
	{
		private SqliteConnection connection;
        public static readonly DateTime LocalUnixEpoch = new DateTime(1970, 1, 1).ToLocalTime();
		
		public SqliteConnection Connection
		{
			get { return connection; }
		}
		
		public Database()
		{
		}
		
		
		public void Open()
		{
			string dbLocation = "URI=file:" + Path.Combine(
						Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
						"tasque/sqlitebackend.db");

			connection = new SqliteConnection(dbLocation);
			connection.Open();
			
			CreateTables();
		}
		
		public void Close()
		{
			connection.Close();
			connection = null;			
		}
		
		public void CreateTables()
		{
			if(!TableExists("Categories")) {
				Console.WriteLine("Creating Categories table");
				ExecuteScalar(@"CREATE TABLE Categories (
					ID INTEGER PRIMARY KEY,
					Name TEXT,
					ExternalID TEXT
				)");
			}

			if(!TableExists("Tasks")) {
				Console.WriteLine("Creating Tasks table");
				ExecuteScalar(@"CREATE TABLE Tasks (
					ID INTEGER PRIMARY KEY,
					Category INTEGER,
					Name TEXT,
					DueDate INTEGER,
					CompletionDate INTEGER,
					Priority INTEGER,
					State INTEGER,
					ExternalID TEXT
				)");
			}

			if(!TableExists("Notes")) {
				Console.WriteLine("Creating Notes table");
				ExecuteScalar(@"CREATE TABLE Notes (
					ID INTEGER PRIMARY KEY,
					Task INTEGER KEY,
					Name TEXT,
					Text TEXT,
					ExternalID TEXT
				)");
			}
		}
		

		public object ExecuteScalar(string command)
        {
        	object resultset;
        	
        	SqliteCommand cmd = connection.CreateCommand();
        	cmd.CommandText = command;
        	resultset = cmd.ExecuteScalar();
        	return resultset;
        }
        
        public int ExecuteNonQuery(string command)		
        {
        	int resultCode;
        	SqliteCommand cmd = connection.CreateCommand();
        	cmd.CommandText = command;
        	resultCode = cmd.ExecuteNonQuery();
        	cmd.Dispose();
        	return resultCode;
        }
        
        public string GetSingleString(string command)
        {
        	string readString = String.Empty;
        	try {
	        	SqliteCommand cmd = connection.CreateCommand();
	        	cmd.CommandText = command;
	        	SqliteDataReader dataReader = cmd.ExecuteReader();
	        	if(dataReader.Read())
	        		readString = dataReader.GetString(0);
	        	else
	        		readString = string.Empty;
	        	dataReader.Close();
	        	cmd.Dispose();
			} catch (Exception e) {
				Logger.Debug("Exception Thrown {0}", e);
			}
        	return readString;
        }
        
        public DateTime GetDateTime(string command)
        {
        	long longValue;
        	DateTime dtValue;
	       	try{
	        	longValue = GetSingleLong(command);
	        	if(longValue == 0)
	        		dtValue = DateTime.MinValue;
	        	else
	        		dtValue = Database.ToDateTime(longValue);
			} catch (Exception e) {
				Logger.Debug("Exception Thrown {0}", e);
				dtValue = DateTime.MinValue;
			}
        	return dtValue;
        }        
        
        public int GetSingleInt(string command)
        {
        	int dtVal = 0;
        	try {        	
	        	SqliteCommand cmd = connection.CreateCommand();
	        	cmd.CommandText = command;
	        	SqliteDataReader dataReader = cmd.ExecuteReader();
	        	if(dataReader.Read())
	        		dtVal = dataReader.GetInt32(0);
	        	else
	        		dtVal = 0;
	        	dataReader.Close();
	        	cmd.Dispose();
			} catch (Exception e) {
				Logger.Debug("Exception Thrown {0}", e);
			}        	
        	return dtVal;
        }  

        public long GetSingleLong(string command)
        {
        	long dtVal = 0;
         	try {       	
	        	SqliteCommand cmd = connection.CreateCommand();
	        	cmd.CommandText = command;
	        	SqliteDataReader dataReader = cmd.ExecuteReader();
	        	if(dataReader.Read())
	        		dtVal = dataReader.GetInt64(0);
	        	else
	        		dtVal = 0;
	        	dataReader.Close();
	        	cmd.Dispose();
			} catch (Exception e) {
				Logger.Debug("Exception Thrown {0}", e);
			} 
        	return dtVal;
        }  

        
		public bool TableExists(string table)
		{
			return Convert.ToInt32(ExecuteScalar(String.Format(@"
				SELECT COUNT(*)
				FROM sqlite_master
				WHERE Type='table' AND Name='{0}'", 
				table))) > 0;
		}

		public static DateTime ToDateTime(long time)
		{
			return FromTimeT(time);
		}

		public static long FromDateTime(DateTime time)
		{
			return ToTimeT(time);
		}

		public static DateTime FromTimeT(long time)
		{
			return LocalUnixEpoch.AddSeconds(time);
		}

		public static long ToTimeT(DateTime time)
		{
			return (long)time.Subtract(LocalUnixEpoch).TotalSeconds;
		}
	}
}
