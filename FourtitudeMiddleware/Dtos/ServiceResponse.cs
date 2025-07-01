namespace FourtitudeMiddleware.Dtos
{
    public class ServiceResponse<T>
    {
        private string _resultMessage;
        private int _result;

        public int Result
        {
            get => _result;
            set => _result = value;
        }

        public string ResultMessage
        {
            get
            {
                return _resultMessage;
            }

            set
            {
                _resultMessage = value;
                if (!string.IsNullOrEmpty(_resultMessage))
                {
                    _result = 0;
                }
            }
        }

        public T Data { get; set; }
    }
} 