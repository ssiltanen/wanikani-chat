[<AutoOpen>]
module Types

open System
open System.Text.Json.Serialization

type Resource<'a> =
    { id: uint
      data_updated_at: DateTime
      data: 'a }

type Collection<'a> =
    { pages:
        {| previous_url: Uri option
           next_url: Uri option
           per_page: uint |}
      total_count: uint
      data_updated_at: DateTime option
      data: 'a[] }

type Assignment =
    { srs_stage: uint // 0..9
      subject_id: uint }

type Vocabulary = { characters: string }
