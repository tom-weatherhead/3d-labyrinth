#!/usr/bin/perl

# Invoke this script as follows:
# ./PrintMySQLDB.pl
# ./PrintMySQLDB.pl 192.168.56.11

# This script requires Cygwin's Perl components "DBI" and "DBD-mysql".

use strict;
use warnings FATAL => qw(all); # Make all warnings fatal.  See page 862.

# See http://stackoverflow.com/questions/10386943/how-to-read-a-mysql-database-in-perl

# To access a database on a remote host: see http://www.perlmonks.org/?node_id=606334
#
# my $dsn = "DBI:mysql:database=$db;host=$host";
# my $dbh = DBI->connect( $dsn, $user, $pass, { RaiseError => 1 }) or die ( "Couldn't connect to database: " . DBI->errstr );

use DBI;

#my $dbh = DBI->connect('dbi:mysql:labyrinth', 'user', 'tomtom7') or die "Connection Error: $DBI::errstr\n";

# Specify the host IP address to force DBI to connect to the database via TCP rather than a Unix socket on the local file system.
# Note that "127.0.0.1" works, but "localhost" does not.
# See http://blog.dt.org/index.php/2009/04/perl-dbi-and-dbdmysql-on-cygwin-connecting-to-a-native-windows-build-of-mysql-on-a-windows-2003-ami-within-amazon-ec2/
my $host = shift || "127.0.0.1";
my $dsn = "DBI:mysql:database=labyrinth;host=$host";
my $dbh = DBI->connect($dsn, "user", "tomtom7", { RaiseError => 1 }) or die "Connection Error: $DBI::errstr\n";

my $sql = "select * from connections";
my $sth = $dbh->prepare($sql);
$sth->execute or die "SQL Error: $DBI::errstr\n";
print "Connections\n";

while (my @row = $sth->fetchrow_array) {
	@row == 4 or die "The connections table does not have 4 columns";
	print "($row[0], $row[1]) to ($row[2], $row[3])\n";
}

$sql = "select * from books";
$sth = $dbh->prepare($sql);
$sth->execute or die "SQL Error: $DBI::errstr\n";
print "\nBooks\n";

while (my @row = $sth->fetchrow_array) {
	@row == 3 or die "The books table does not have 3 columns";
	print "($row[0], $row[1]) contains '$row[2]'\n";
}

$sql = "SELECT level2, room2 FROM connections WHERE level1 = ? and room1 = ?";
$sth = $dbh->prepare($sql);
$sth->execute(14, 6) or die "SQL Error: $DBI::errstr\n";
print "\nRooms connected to (14, 6)\n";

while (my @row = $sth->fetchrow_array()) {
	@row == 2 or die "The row does not have 2 columns";
	print "($row[0], $row[1])\n";
}
