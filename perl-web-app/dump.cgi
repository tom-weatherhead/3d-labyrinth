#!/usr/bin/perl

# The labyrinthine abbey library as a Perl Web app - June 23, 2014
# This script dumps (displays the contents of) the labyrinth.

# See http://template-toolkit.org/docs/tutorial/Web.html

use strict;
use warnings FATAL => qw(all); # Make all warnings fatal.  See page 862.  Or: use warnings qw/FATAL all/;

use CGI qw/:standard/; # Load standard CGI routines.
#use CGI qw/:all/;
use DBI;
use HTML::Table;
use Template;

#my $cgi = CGI->new;
#my $foo = param('foo') || 'undefined'; # param() can return a scalar or a list.
my $connectionsHTMLTable = new HTML::Table(-cols => 2, -head => ["From", "To"]);
my $booksHTMLTable = new HTML::Table(-cols => 2, -head => ["Room", "Book"]);
my $dsn = "DBI:mysql:database=labyrinth;host=localhost";
my $dbh = DBI->connect($dsn, "user", "tomtom7", { RaiseError => 1 }) or die "Connection Error: $DBI::errstr\n";

my $sql = "select * from connections";
my $sth = $dbh->prepare($sql);
$sth->execute or die "SQL Error: $DBI::errstr\n";

while (my @row = $sth->fetchrow_array) {
	@row == 4 or die "The connections table does not have 4 columns";
	$connectionsHTMLTable->addRow("($row[0], $row[1])", "($row[2], $row[3])");
}

$sql = "select * from books";
$sth = $dbh->prepare($sql);
$sth->execute or die "SQL Error: $DBI::errstr\n";

while (my @row = $sth->fetchrow_array) {
	@row == 3 or die "The books table does not have 3 columns";
	$booksHTMLTable->addRow("($row[0], $row[1])", $row[2]);
}

my $vars = {
	"connectionsHTMLTable" => $connectionsHTMLTable,
	"booksHTMLTable" => $booksHTMLTable
};

my $template = Template->new({
	INCLUDE_PATH => "/var/www/html/perl/labyrinth" #,
	# PRE_PROCESS  => "config"
});

print header(-type => "text/html",
             -charset => "utf-8");

$template->process("dump.tt", $vars)
	|| die $template->error();
