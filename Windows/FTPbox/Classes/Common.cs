﻿/* License
 * This file is part of FTPbox - Copyright (C) 2012 ftpbox.org
 * FTPbox is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published 
 * by the Free Software Foundation, either version 3 of the License, or (at your option) any later version. This program is distributed 
 * in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. 
 * See the GNU General Public License for more details. You should have received a copy of the GNU General Public License along with this program. 
 * If not, see <http://www.gnu.org/licenses/>.
 */
/* Common.cs
 * Commonly used functions are called from this class.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FTPbox;

namespace FTPboxLib
{
    public static class Common
    {
        #region Fields

        public static List<string> LocalFolders = new List<string>();       //Used to store all the local folders at all times
        public static List<string> LocalFiles = new List<string>();         //Used to store all the local files at all times
        
        public static IgnoreList IgnoreList = new IgnoreList();	            //list of ignored folders
        public static FileLog FileLog = new FileLog();                      //the file log
        public static Translations Languages = new Translations();          //Used to grab the translations from the translations.xml file

        #endregion

        #region Properties

        /// <summary>
        /// Encrypt the given password. Used to store passwords encrypted in the config file.
        /// </summary>
        public static string Encrypt(string password)
        {
            return Utilities.Encryption.AESEncryption.Encrypt(password, Profile.DecryptionPassword, Profile.DecryptionSalt);
        }

        /// <summary>
        /// Decrypt the given encrypted password.
        /// </summary>
        public static string Decrypt(string encrypted)
        {
            return Utilities.Encryption.AESEncryption.Decrypt(encrypted, Profile.DecryptionPassword, Profile.DecryptionSalt);
        }

        /// <summary>
        /// Gets the common path of both local and remote directories.
        /// </summary>
        /// <returns>
        /// The common path, using forward slashes ( / )
        /// </returns>
        /// <param name='s'>
        /// The full path to be 'shortened'
        /// </param>
        /// <param name='fromLocal'>
        /// True if the given path is in local format.
        /// </param>
        public static string GetComPath(string s, bool fromLocal)
        {
            string cp = s;

            if (fromLocal)
            {
                if (cp.StartsWith(Profile.LocalPath))
                {
                    cp = cp.Substring(Profile.LocalPath.Length);
                    cp = cp.Replace(@"\", @"/");
                }
            }
            else
            {
                cp = (cp.StartsWith("/")) ? cp.Substring(1) : cp;
                //Log.Write(l.Debug, "without slash: {0}", cp);
                if (Profile.Protocol == FtpProtocol.SFTP && Profile.HomePath != "")
                {
                    if (cp.StartsWith(Profile.HomePath))
                        cp = cp.Substring(Profile.HomePath.Length + 1);
                    if (cp.StartsWith("/" + Profile.HomePath))
                        cp = cp.Substring(Profile.HomePath.Length + 2);
                }
                if (cp.StartsWith(Profile.RemotePath))
                {
                    cp = cp.Substring(Profile.RemotePath.Length);
                }
            }

            if (cp.StartsWith("/") || cp.StartsWith(@"\"))
                cp = cp.Substring(1);
            if (cp.StartsWith("./"))
                cp = cp.Substring(2);

            if (string.IsNullOrWhiteSpace(cp))
                cp = "/";

            return cp;
        }

        /// <summary>
        /// Checks the type of item in the specified path (the item must exist)
        /// </summary>
        /// <param name="p">The path to check</param>
        /// <returns>true if the specified path is a file, false if it's a folder</returns>
        public static bool PathIsFile(string p)
        {
            try
            {
                FileAttributes attr = File.GetAttributes(p);
                return (attr & FileAttributes.Directory) != FileAttributes.Directory;
            }
            catch
            {
                return !LocalFolders.Contains(p);
            }
        }

        /// <summary>
        /// Removes the part until the last slash from the provided string.
        /// </summary>
        /// <param name="path">the path to the item</param>
        /// <returns>name of the item</returns>
        public static string _name(string path)
        {
            if (path.Contains("/"))
                path = path.Substring(path.LastIndexOf("/"));
            if (path.Contains(@"\"))
                path = path.Substring(path.LastIndexOf(@"\"));
            if (path.StartsWith("/") || path.StartsWith(@"\"))
                path = path.Substring(1);
            return path;
        }

        /// <summary>
        /// Puts ~ftpb_ to the beginning of the filename in the given item's path
        /// </summary>
        /// <param name="cpath">the given item's common path</param>
        /// <returns>Temporary path to item</returns>
        public static string _tempName(string cpath)
        {
            if (!cpath.Contains("/") && !cpath.Contains(@"\"))
                return string.Format("~ftpb_{0}", cpath);

            string parent = cpath.Substring(0, cpath.LastIndexOf("/"));
            string temp_name = string.Format("~ftpb_{0}", _name(cpath));

            return string.Format("{0}/{1}", parent, temp_name);
        }

        /// <summary>
        /// Puts ~ftpb_ to the beginning of the filename in the given item's local path
        /// </summary>
        /// <param name="lpath">the given item's local path</param>
        /// <returns>Temporary local path to item</returns>
        public static string _tempLocal(string lpath)
        {
            string parent = lpath.Substring(0, lpath.LastIndexOf(@"\"));            

            return string.Format(@"{0}\~ftpb_{1}", parent, _name(lpath));
        }

        /// <summary>
        /// removes slashes from beggining and end of paths
        /// </summary>
        /// <param name="x">the path from which to remove the slashes</param>
        /// <returns>path without slashes</returns>
        public static string noSlashes(string x)
        {
            string noslashes = x;
            if (noslashes.StartsWith(@"\"))            
                noslashes = noslashes.Substring(1, noslashes.Length - 1);            
            if (noslashes.EndsWith(@"/") || noslashes.EndsWith(@"\"))            
                noslashes = noslashes.Substring(0, noslashes.Length - 1);
            
            return noslashes;
        }

        /// <summary>
        /// Whether the specified path should be synced. Used in selective sync and to avoid syncing the webUI folder, temp files and invalid file/folder-names.
        /// </summary>
        public static bool ItemGetsSynced(string name)
        {
            if (name.EndsWith("/"))
                name = name.Substring(0, name.Length - 1);
            string aName = (name.Contains("/")) ? name.Substring(name.LastIndexOf("/")) : name;         //the actual name of the file in the given path. (removes the path from the beginning of the given string)
            if (aName.StartsWith("/"))
                aName = aName.Substring(1);

            bool b = !(IgnoreList.isIgnored(name)
                || name.Contains("webint") || name.EndsWith(".") || name.EndsWith("..")                 //web interface, current and parent folders are ignored
                || aName == ".ftpquota" || aName == "error_log" || aName.StartsWith(".bash")            //server files are ignored
                || !IsAllowedFilename(aName)                                                            //checks characters not allowed in windows file/folder names
                || aName.StartsWith("~ftpb_")                                                           //FTPbox-generated temporary files are ignored
                );
            
            if (!b)
                Log.Write(l.Debug, "Item {0} gets synced: No", name);

            return b;
        }       

        /// <summary>
        /// Checks a filename for chars that wont work with most servers
        /// </summary>
        private static bool IsAllowedFilename(string name)
        {
            return name.ToCharArray().All(IsAllowedChar);
        }

        /// <summary>
        /// Checks if a char is allowed, based on the allowed chars for filenames
        /// </summary>
        private static bool IsAllowedChar(char ch)
        {
            var forbidden = new char[] { '?', '"', '*', ':', '<', '>', '|' };
            return !forbidden.Any(ch.Equals);
        }

        /// <summary>
        /// Checks if a file's parent folder contains spaces. If yes, the specified file should not be deleted locally. 
        /// This is temprorary, until a fix for spaces is released.
        /// </summary>
        /// <param name="cpath">common path to file.</param>
        /// <returns></returns>
        public static bool ParentFolderHasSpace(string cpath)
        {
            if (cpath.Length <= _name(cpath).Length)
                return false;

            cpath = cpath.Substring(0, cpath.Length - _name(cpath).Length);

            return cpath.Contains(" ");
        }

        /// <summary>
        /// Get the HTTP link to a file
        /// </summary>
        /// <param name='cpath'>
        /// The common path to the file/folder.
        /// </param>
        public static string GetHttpLink(string file)
        {
            string cpath = GetComPath(file, true);

            string newlink = noSlashes(Profile.HttpPath) + @"/";

            if (!noSlashes(newlink).StartsWith("http://") && !noSlashes(newlink).StartsWith("https://"))
            {
                newlink = @"http://" + newlink;
            }
            
            if (newlink.EndsWith("/"))
                newlink = newlink.Substring(0, newlink.Length - 1);

            if (cpath.StartsWith("/"))
                cpath = cpath.Substring(1);

            newlink = string.Format("{0}/{1}", newlink, cpath);
            newlink = newlink.Replace(@" ", @"%20");

            Log.Write(l.Debug, "**************");
            Log.Write(l.Debug, "HTTP Link is: " + newlink);
            Log.Write(l.Debug, "**************");

            return newlink;
        }

        /// <summary>
        /// Checks if a file is still being used (hasn't been completely transfered to the folder)
        /// </summary>
        /// <param name="path">The file to check</param>
        /// <returns><c>True</c> if the file is being used, <c>False</c> if now</returns>
        public static bool FileIsUsed(string path)
        {
            FileStream stream = null;
            string name = null;

            try
            {
                var fi = new FileInfo(path);
                name = fi.Name;
                stream = fi.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            catch
            {
                if (name != null)
                    Log.Write(l.Debug, "File {0} is locked: True", name);
                return true;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }
            if (!string.IsNullOrWhiteSpace(name))
                Log.Write(l.Debug, "File {0} is locked: False", name);
            return false;
        }

        #endregion

        #region Functions

        /// <summary>
        /// removes an item from the log
        /// </summary>
        /// <param name="cPath">name to remove</param>
        public static void RemoveFromLog(string cPath)
        {
            if (FileLog.Contains(cPath))
                FileLog.Remove(cPath);
            Settings.SaveProfile();
        }

        /// <summary>
        /// displays details of the thrown exception in the console
        /// </summary>
        /// <param name="error"></param>
        public static void LogError(Exception error)
        {
            Log.Write(l.Error, "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
            Log.Write(l.Error, "Message: {0}", error.Message);
            Log.Write(l.Error, "--");
            Log.Write(l.Error, "StackTrace: {0}", error.StackTrace);
            Log.Write(l.Error, "--");
            Log.Write(l.Error, "Source: {0}", error.Source);
            Log.Write(l.Error, "--");
            foreach (KeyValuePair<string, string> s in error.Data)
                Log.Write(l.Error, "key: {0} value: {1}", s.Key, s.Value);
            Log.Write(l.Error, "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
        }

        #endregion
    }
}
