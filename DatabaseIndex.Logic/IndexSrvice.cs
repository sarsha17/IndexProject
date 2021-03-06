﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DatabaseIndex.Entities;

namespace DatabaseIndex.Logic
{
    public class IndexService<TRow>
    {
        private IEnumerable<TRow> _tableData;
        private Dictionary<Type, IndexHandler<IComparable, TRow>> _IndexeHandlers;

        public IndexService(IEnumerable<TRow> tableData)
        {
            UpdateTableSource(tableData);
            _IndexeHandlers = new Dictionary<Type, IndexHandler<IComparable, TRow>>();
        }

        /// <summary>
        /// Create an index on a table's property
        /// </summary>
        /// <param name="key">index unique key</param>
        /// <param name="propertyFunc">func to extract the property</param>
        public void CreateIndex(string key, Func<TRow, IComparable> propertyFunc)
        {
            var item = propertyFunc(_tableData.First());
            Type itemType = item.GetType();

            if (!_IndexeHandlers.ContainsKey(itemType))
            {
                _IndexeHandlers.Add(itemType, new IndexHandler<IComparable, TRow>(_tableData));
                _IndexeHandlers[itemType].CreateIndex(key, propertyFunc);
            }

            else
            {
                IndexHandler<IComparable, TRow> handler = _IndexeHandlers[itemType];
                handler.CreateIndex(key, propertyFunc);
            }
        }

        /// <summary>
        /// Remove an index
        /// </summary>
        /// <param name="indexkey">index key</param>
        /// <returns>true if found and removed</returns>
        public bool DropIndex(string indexkey)
        {
            IndexHandler<IComparable, TRow> handler;
            if (ContainsIndex(indexkey, out handler))
            {
                return handler.DropIndex(indexkey);
            }

            return false;
        }

        /// <summary>
        /// Retrieve data range using indexes
        /// </summary>
        /// <param name="indexkey">index key</param>
        /// <param name="from">from value</param>
        /// <param name="to">to value</param>
        /// <returns>every row found in the values range</returns>
        public IEnumerable<TRow> RetrieveData(string indexkey, IComparable from, IComparable to) //Match query on a single field, several values
        {
            IndexHandler<IComparable, TRow> handler;
            if (ContainsIndex(indexkey, out handler))
            {
                return handler.Retrieve(indexkey, from, to);
            }

            return null;
        }

        /// <summary>
        /// Retrieve data using indexes
        /// </summary>
        /// <param name="indexkey">index key</param>
        /// <param name="value">value to find</param>
        /// <returns>every row contains the value</returns>
        public IEnumerable<TRow> RetrieveData(string indexkey, IComparable value) //Match query on a single field, one value
        {
            return RetrieveData(indexkey, value, value);
        }

        public IEnumerable<TRow> RetrieveData(string firstField, string secondField, IComparable value, SearchOperation operation) 
        {
            switch (operation)
            {
                case SearchOperation.Or:
                    return OrMatchOnTwoFields(firstField, secondField, value);
                case SearchOperation.And:

                default:
                    return OrMatchOnTwoFields(firstField, secondField, value);
            }
        }

        private IEnumerable<TRow> OrMatchOnTwoFields(string firstField, string secondField, IComparable value) //An OR of a match query on 2 different fields, one value
        {
            List<TRow> result = RetrieveData(firstField, value).ToList();
            result.InsertRange(0, RetrieveData(secondField, value));
            result = result.Distinct().ToList(); //remove duplicates
            return result;
        }

        //Checks if a indexhandler exists for a given key
        //returns as out parameter the handler which contains the found key
        private bool ContainsIndex(string indexkey, out IndexHandler<IComparable, TRow> handler)
        {
            foreach (var savedHandler in _IndexeHandlers.Values)
            {
                if (savedHandler.ContainsKey(indexkey))
                {
                    handler = savedHandler;
                    return true;
                }
            }
            handler = null;
            return false;
        }


        /// <summary>
        /// Check if an index exists
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool ContainsIndex(string key)
        {
            foreach (var savedHandler in _IndexeHandlers.Values)
            {
                if (savedHandler.ContainsKey(key))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Update the table data with new table.
        /// Please note, updating the data will also erase all indexes.
        /// </summary>
        /// <param name="newTable">new table data</param>
        public void UpdateTableSource(IEnumerable<TRow> newTable)
        {
            if (newTable != null)
            {
                _tableData = newTable;
                _IndexeHandlers = new Dictionary<Type, IndexHandler<IComparable, TRow>>();
            }
            else
            {
                throw new NullReferenceException("Table data can not be null");
            }
        }
    }
}
