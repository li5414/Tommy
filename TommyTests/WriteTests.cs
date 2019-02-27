﻿using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tommy;

namespace TommyTests
{
    [TestClass]
    public class WriteTests
    {
        [TestMethod]
        public void ObjectConstructTest()
        {
            var node = new TomlTable
            {
                ["hello"] = new TomlTable
                {
                    IsSection = true,
                    Comment = "This table is used for Hello, world -commands!",
                    ["key"] = "wew",
                    ["test"] = new TomlTable
                    {
                        IsSection = true,
                        Comment = "This is another section table!",
                        ["foo"] = "Value",
                        ["bar"] = new TomlInteger
                        {
                            Comment = "How many bars there are to eat",
                            Value = 10
                        }
                    }
                },
                ["value"] = 10.0,
                ["table-test"] = new TomlArray
                {
                    IsTableArray = true,
                    [0] = new TomlTable
                    {
                        ["wew"] = "wew"
                    },
                    [1] = new TomlTable
                    {
                        ["wew"] = "veemo"
                    },
                    [2] = new TomlTable
                    {
                        ["wew"] = "woomy"
                    }
                }
            };

            using (StringWriter sw = new StringWriter())
            {
                node.ToTomlString(sw);

                string s = sw.ToString();
                File.WriteAllText("out.toml", s);
            }
        }
    }
}
