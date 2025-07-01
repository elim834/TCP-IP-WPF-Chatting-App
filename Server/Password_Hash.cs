using System;
using System.Text;
using System.Security.Cryptography;
using System.Windows;
using MySql.Data.MySqlClient;

namespace Server;

public class PasswordHash
{
    public class Program
    {
        private static string connStr = "server=localhost;user=root;password=AtesBebis_20;database=rider_dbb;";
        public class Result
        {
            public bool Success { get; set; }
            public string? Message { get; set; } 
            public int? UserId { get; set; }     
            public string? Username { get; set; }
        }

        private static string ComputeSha256Hash(string plainpassword)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(plainpassword));

                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
        
        public static Result SignInRequest(string username, string plainpassword)
        {
            try
            {
                string passwordHash  = ComputeSha256Hash(plainpassword);

                using var conn = new MySqlConnection(connStr);
                conn.Open();
                
                
                string query = "SELECT Id, Username FROM Users WHERE username = @username AND password = @password";
                using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@username", username);
                cmd.Parameters.AddWithValue("@password", passwordHash );

                
                using var reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    int foundUserId = Convert.ToInt32(reader["Id"]);
                    string foundUsername = reader["Username"].ToString();
                    reader.Close();
                    
                    return new Result
                    {
                        Success = true,
                        UserId = foundUserId,
                        Username = foundUsername
                        
                    };
                }

                return new Result
                {
                    Success = false,
                    Message = "Invalid username or password."
                };
            }
            catch (Exception e)
            {
                return new Result
                {
                    Success = false,
                    Message = "A SignInRequest error occurred: " + e.Message
                };
            }

        
        }
        

         public static Result SignUpRequest(string username, string plainpassword)
        {
            try
            {
                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(plainpassword))
                {
                    return new Result()
                    {
                        Success = false,
                        Message = "Invalid username or password. Please use a different username/password."
                    };
                }
                
                using var con = new MySqlConnection(connStr);
                con.Open();

                string checkUserQuery = "SELECT COUNT(*) FROM Users WHERE Username = @username";
                using var checkCmd = new MySqlCommand(checkUserQuery, con);
                checkCmd.Parameters.AddWithValue("@username", username);

                long userExists = (long)checkCmd.ExecuteScalar();

                if (userExists > 0)
                {
                    return new Result()
                    {
                        Success = false,
                        Message= "This username is taken. Please use a different username."
                    };
                }
                
                string passwordHash = ComputeSha256Hash(plainpassword);

                string insertQuery = "INSERT INTO Users (Username, Password) VALUES (@username, @password)";
                using var insertCmd = new MySqlCommand(insertQuery, con);
                insertCmd.Parameters.AddWithValue("@username", username);
                insertCmd.Parameters.AddWithValue("@password", passwordHash);
                insertCmd.ExecuteNonQuery();
                
                string getIdQuery = "SELECT LAST_INSERT_ID()";
                using var getIdCmd = new MySqlCommand(getIdQuery, con);
                int newUserId = Convert.ToInt32(getIdCmd.ExecuteScalar());
                
                return new Result()
                {
                    Success = true,
                    Message = "The user has been added to the database.",
                    UserId = newUserId,
                    Username = username
                };
            }
            catch (Exception e)
            {
                return new Result()
                {
                    Success = false,
                    Message = "SignUpRequest Error: " + e.Message
                };
            }
        }
    }
}