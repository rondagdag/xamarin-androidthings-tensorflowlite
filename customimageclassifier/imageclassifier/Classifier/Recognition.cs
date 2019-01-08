namespace imageclassifier
{
    internal class Recognition
    {

        /*
     * A unique identifier for what has been recognized. Specific to the class, not the instance of
     * the object.
     */
        string _id;

        /*
         * Display name for the recognition.
         */
        string _title;

        /*
         * A sortable score for how good the recognition is relative to others. Higher should be better.
         */
        float? _confidence;

        public Recognition(
                 string id, string title, float confidence)
        {
            this._id = id;
            this._title = title;
            this._confidence = confidence;
        }

        public string GetId()
        {
            return _id;
        }

        public string GetTitle()
        {
            return _title;
        }

        public float GetConfidence()
        {
            return _confidence == null ? 0f : _confidence.Value;
        }


        public override string ToString()
        {
            string resultString = "";
            if (_id != null)
            {
                resultString += "[" + _id + "] ";
            }

            if (_title != null)
            {
                resultString += _title + " ";
            }

            if (_confidence != null)
            {
                resultString += string.Format("({0}%) ", _confidence * 100.0f);
            }

            return resultString.Trim();
        }
    }
}