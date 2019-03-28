# ExpertTools
The main purpose of this toolkit it`s helping to the 1C experts in detection of the 1C applications problem places.
Project is in the development stage. 

<h2>Queries analyze:</h5>
Application collects the tech log data and extended events data, creates MD5 hash for each request statement and inserts theese records into it`s own database. Table with the tech log data contains first and last code lines for each request statement. Extended events data contains performance indicators for each reques statement (such as cpu_time, duration, logical_reads, phisycal_reads and writes). You can get ordered request list by any performance indicator and for each request to get first and last context lines by statement hash, get query plan by plan_handle value.
