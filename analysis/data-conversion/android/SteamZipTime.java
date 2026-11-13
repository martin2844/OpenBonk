package com.openbonk.dataprep;

import java.util.GregorianCalendar;
import java.util.Locale;
import java.util.TimeZone;

/** ZIP records use local time; keep their fields stable across time zones. */
final class SteamZipTime {
 static long fixed() {
  GregorianCalendar date=new GregorianCalendar(TimeZone.getDefault(),Locale.ROOT);
  date.clear();
  date.set(1980,0,1,1,0,0);
  return date.getTimeInMillis();
 }
 private SteamZipTime(){}
}
