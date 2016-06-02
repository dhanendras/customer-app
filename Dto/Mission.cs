using System;
using customerapp.Dto;
using System.Collections.Generic;

namespace customerapp
{
	public class Mission
	{

		public String Id { get; set; }

		public String State { get; set; }

		public Route Route { get; set; }

		public OrderProduct OrderProduct { get; set; }

		public List<DroneInfo> DroneInfos{ get; set; }
	}

}

