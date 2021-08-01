db = db.getSiblingDB('admin');
db.createUser( { user: "mops",
          pwd: "123321",
          roles: [ "userAdminAnyDatabase",
                   "dbAdminAnyDatabase",
                   "readWriteAnyDatabase"
		 ]
});