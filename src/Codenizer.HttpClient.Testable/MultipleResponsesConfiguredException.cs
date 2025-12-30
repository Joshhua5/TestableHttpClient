using System;
using System.Runtime.Serialization;

namespace Codenizer.HttpClient.Testable
{
    /// <summary>
    /// Thrown when multiple responses were configured for the same HTTP request
    /// </summary>
    [Serializable]
    public class MultipleResponsesConfiguredException : Exception
    {
        internal MultipleResponsesConfiguredException(int numberOfResponses, string pathAndQuery) 
            : base("Multiple responses configured for the same path and query string")
        {
            NumberOfResponses = numberOfResponses;
            PathAndQuery = pathAndQuery;
        }

        /// <inheritdoc />
        [Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.")]
        protected MultipleResponsesConfiguredException(SerializationInfo info, StreamingContext context)
             : base(info, context)
        {
            NumberOfResponses = info.GetInt32("numberOfResponses");
            PathAndQuery = info.GetString("pathAndQuery");
        }

        /// <inheritdoc />
        [Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.")]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("numberOfResponses", NumberOfResponses);
            info.AddValue("pathAndQuery", PathAndQuery);

            base.GetObjectData(info, context);
        }

        /// <summary>
        /// Gets the number of responses configured for the HTTP request
        /// </summary>
        public int NumberOfResponses
        {
            get
            {
                if (Data.Contains(nameof(NumberOfResponses)) && Data[nameof(NumberOfResponses)] is int count)
                {
                    return count;
                }

                return 0;
            }
            private set
            {
                if (Data.Contains(nameof(NumberOfResponses)))
                {
                    Data[nameof(NumberOfResponses)] = value;
                }
                else
                {
                    Data.Add(nameof(NumberOfResponses), value);
                }
            }
        }

        /// <summary>
        /// Gets the path and query string of the responses configured for the HTTP request
        /// </summary>
        public string? PathAndQuery
        {
            get
            {
                if (Data.Contains(nameof(PathAndQuery)))
                {
                    return Data[nameof(PathAndQuery)] as string;
                }

                return null;
            }
            private set
            {
                if (Data.Contains(nameof(PathAndQuery)))
                {
                    Data[nameof(PathAndQuery)] = value;
                }
                else
                {
                    Data.Add(nameof(PathAndQuery), value);
                }
            }
        }
    }
}